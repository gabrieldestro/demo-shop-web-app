import { Component, inject, OnInit } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { MatButtonModule } from '@angular/material/button';
import {
  MAT_DIALOG_DATA, MatDialogActions, MatDialogClose,
  MatDialogContent, MatDialogRef, MatDialogTitle
} from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { MatIconModule } from '@angular/material/icon';
import { FormsModule } from '@angular/forms';
import { Product } from '../../../shared/models/product';
import { AdminService } from '../../../core/services/admin.service';
import { MatSnackBar } from '@angular/material/snack-bar';

@Component({
  selector: 'app-product-dialog',
  imports: [
    MatButtonModule, MatDialogTitle, MatDialogContent, MatDialogActions,
    MatDialogClose, MatFormFieldModule, MatInputModule, MatSelectModule,
    MatAutocompleteModule, MatIconModule, FormsModule
  ],
  templateUrl: './product-dialog.component.html',
  styleUrl: './product-dialog.component.scss'
})
export class ProductDialogComponent implements OnInit {
  private adminService = inject(AdminService);
  private snackbar = inject(MatSnackBar);
  private dialogRef = inject(MatDialogRef<ProductDialogComponent>);
  data: { product?: Product } = inject(MAT_DIALOG_DATA);

  isEdit = false;
  model = {
    name: '',
    description: '',
    price: 0,
    pictureUrl: '',
    type: '',
    brand: '',
    quantityInStock: 1
  };
  brands: string[] = [];
  types: string[] = [];
  selectedFile: File | null = null;
  imagePreview: string | null = null;
  uploading = false;

  ngOnInit() {
    this.loadBrands();
    this.loadTypes();
    if (this.data.product) {
      this.isEdit = true;
      const p = this.data.product;
      this.model = {
        name: p.name,
        description: p.description,
        price: p.price,
        pictureUrl: p.pictureUrl,
        type: p.type,
        brand: p.brand,
        quantityInStock: p.quantityInStock
      };
      this.imagePreview = p.pictureUrl;
    }
  }

  loadBrands() {
    this.adminService.getBrands().subscribe({
      next: brands => this.brands = brands
    });
  }

  loadTypes() {
    this.adminService.getTypes().subscribe({
      next: types => this.types = types
    });
  }

  onFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files[0]) {
      this.selectedFile = input.files[0];
      const reader = new FileReader();
      reader.onload = () => this.imagePreview = reader.result as string;
      reader.readAsDataURL(this.selectedFile);
    }
  }

  async save() {
    if (!this.model.name || !this.model.description || !this.model.brand || !this.model.type) {
      this.snackbar.open('Please fill all required fields', 'Close', { duration: 3000 });
      return;
    }

    this.uploading = true;

    try {
      if (this.selectedFile) {
        const result = await firstValueFrom(this.adminService.uploadImage(this.selectedFile));
        this.model.pictureUrl = result.url;
      }

      if (this.isEdit) {
        await firstValueFrom(this.adminService.updateProduct(this.data.product!.id, this.model));
        this.snackbar.open('Product updated', 'Close', { duration: 3000 });
      } else {
        await firstValueFrom(this.adminService.createProduct(this.model));
        this.snackbar.open('Product created', 'Close', { duration: 3000 });
      }

      this.dialogRef.close(true);
    } catch (err: any) {
      this.snackbar.open(err.error?.title || 'Error saving product', 'Close', { duration: 3000 });
    } finally {
      this.uploading = false;
    }
  }
}
