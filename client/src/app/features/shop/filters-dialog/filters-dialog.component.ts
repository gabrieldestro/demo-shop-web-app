import { Component, inject } from '@angular/core';
import {MatDivider} from '@angular/material/divider';
import {MatListOption, MatSelectionList} from '@angular/material/list';
import { MatButton } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-filters-dialog',
  imports: [
    MatDivider,
    MatSelectionList,
    MatListOption,
    MatButton,
    FormsModule
  ],
  templateUrl: './filters-dialog.component.html',
  styleUrl: './filters-dialog.component.scss'
})
export class FiltersDialogComponent {
  private dialogRef = inject(MatDialogRef<FiltersDialogComponent>);
  data = inject<{selectedBrands: string[], selectedTypes: string[], brands: string[], types: string[]}>(MAT_DIALOG_DATA);

  selectedBrands: string[] = this.data.selectedBrands;
  selectedTypes: string[] = this.data.selectedTypes;

  applyFilters() {
    this.dialogRef.close({
      selectedBrands: this.selectedBrands,
      selectedTypes: this.selectedTypes
    });
  }

  clearFilters() {
    this.selectedBrands = [];
    this.selectedTypes = [];
    this.applyFilters();
  }
}