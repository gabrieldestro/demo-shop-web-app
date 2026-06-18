import { Component, inject, OnInit } from '@angular/core';
import { AdminService, CouponDto } from '../../core/services/admin.service';
import { Order } from '../../shared/models/order';
import { Product } from '../../shared/models/product';
import { MatTableDataSource, MatTableModule } from '@angular/material/table';
import { OrderParams } from '../../shared/models/orderParams';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatLabel, MatSelectChange, MatSelectModule } from '@angular/material/select';
import { MatTabsModule } from '@angular/material/tabs';
import { DatePipe, CurrencyPipe, PercentPipe } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { RouterLink } from '@angular/router';
import { MatTooltipModule } from '@angular/material/tooltip';
import { DialogService } from '../../core/services/dialog.service';
import { FormsModule } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';

@Component({
  selector: 'app-admin',
  imports: [
    MatTabsModule,
    MatTableModule,
    MatPaginatorModule,
    MatButtonModule,
    MatIconModule,
    MatTooltipModule,
    DatePipe,
    RouterLink,
    MatLabel,
    MatSelectModule,
    CurrencyPipe,
    FormsModule,
    PercentPipe,
    MatSlideToggleModule,
    MatFormFieldModule,
    MatInputModule
  ],
  templateUrl: './admin.component.html',
  styleUrl: './admin.component.scss'
})
export class AdminComponent implements OnInit {
  private adminService = inject(AdminService);
  private dialogService = inject(DialogService);
  private snackbar = inject(MatSnackBar);
  displayedColumns: string[] = ['id', 'buyerEmail', 'orderDate', 'total', 'status', 'action'];
  dataSource = new MatTableDataSource<Order>([]);
  orderParams = new OrderParams();
  totalItems = 0;
  statusOptions = ['All', 'PaymentReceived', 'PaymentMismatch', 'Refunded', 'Pending']

  products: Product[] = [];
  filteredProducts: Product[] = [];
  editingStockId: number | null = null;
  editingStockValue: number = 0;
  stockSearchQuery = '';

  get totalProducts() { return this.products.length; }
  get outOfStockCount() { return this.products.filter(p => p.quantityInStock === 0).length; }
  get lowStockCount() { return this.products.filter(p => p.quantityInStock > 0 && p.quantityInStock <= 5).length; }

  // Coupon management
  coupons: CouponDto[] = [];
  newCouponCode = '';
  newCouponDiscount = 0;
  editingCouponId: number | null = null;
  editingCouponCode = '';
  editingCouponDiscount = 0;
  showAddCoupon = false;

  ngOnInit(): void {
    this.loadOrders();
    this.loadProducts();
    this.loadCoupons();
  }

  loadOrders(): void {
    this.adminService.getOrders(this.orderParams).subscribe({
      next: response => {
        if (response.data) {
          this.dataSource.data = response.data;
          this.totalItems = response.count;
        }
      }
    });
  }

  onPageChange(event: PageEvent): void {
    this.orderParams.pageNumber = event.pageIndex + 1;
    this.orderParams.pageSize = event.pageSize;
    this.loadOrders();
  }

  onFilterSelect(event: MatSelectChange) {
    this.orderParams.filter = event.value;
    this.orderParams.pageNumber = 1;
    this.loadOrders();
  }

  async openConfirmDialog(id: number) {
    const confirmed = await this.dialogService.confirm(
      'Confirm refund',
      'Are you sure you want to issue this refund? This cannot be undone'
    )

    if (confirmed) {
      this.refundOrder(id);
    }
  }

  refundOrder(id: number): void {
    this.adminService.refundOrder(id).subscribe({
      next: order => {
        this.dataSource.data = this.dataSource.data.map(o => o.id === id ? order : o)
      }
    })
  }

  loadProducts(): void {
    this.adminService.getProducts().subscribe({
      next: products => {
        this.products = products;
        this.filteredProducts = products;
      }
    });
  }

  onSearchChange(value: string): void {
    this.stockSearchQuery = value;
    const q = value.toLowerCase().trim();
    if (!q) {
      this.filteredProducts = [...this.products];
    } else {
      this.filteredProducts = this.products.filter(p =>
        p.name.toLowerCase().includes(q) ||
        p.brand.toLowerCase().includes(q) ||
        p.type.toLowerCase().includes(q)
      );
    }
  }

  startEditStock(product: Product): void {
    this.editingStockId = product.id;
    this.editingStockValue = product.quantityInStock;
  }

  cancelEditStock(): void {
    this.editingStockId = null;
  }

  saveStock(product: Product): void {
    this.adminService.updateProductStock(product.id, this.editingStockValue).subscribe({
      next: () => {
        product.quantityInStock = this.editingStockValue;
        this.editingStockId = null;
        this.snackbar.open('Stock updated', 'Close', { duration: 3000 });
      },
      error: () => {
        this.snackbar.open('Error updating stock', 'Close', { duration: 3000 });
      }
    });
  }

  loadCoupons(): void {
    this.adminService.getCoupons().subscribe({
      next: coupons => this.coupons = coupons
    });
  }

  addCoupon(): void {
    if (!this.newCouponCode.trim() || this.newCouponDiscount <= 0) return;
    this.adminService.createCoupon(this.newCouponCode.trim().toUpperCase(), this.newCouponDiscount).subscribe({
      next: coupon => {
        this.coupons = [...this.coupons, coupon];
        this.newCouponCode = '';
        this.newCouponDiscount = 0;
        this.showAddCoupon = false;
        this.snackbar.open('Coupon created', 'Close', { duration: 3000 });
      },
      error: (err) => {
        this.snackbar.open(err.error?.title || 'Error creating coupon', 'Close', { duration: 3000 });
      }
    });
  }

  startEditCoupon(coupon: CouponDto): void {
    this.editingCouponId = coupon.id;
    this.editingCouponCode = coupon.code;
    this.editingCouponDiscount = coupon.discountPercent;
  }

  cancelEditCoupon(): void {
    this.editingCouponId = null;
  }

  saveEditCoupon(): void {
    if (!this.editingCouponCode.trim() || this.editingCouponDiscount <= 0) return;
    this.adminService.updateCoupon(this.editingCouponId!, this.editingCouponCode.trim().toUpperCase(), this.editingCouponDiscount).subscribe({
      next: coupon => {
        this.coupons = this.coupons.map(c => c.id === coupon.id ? coupon : c);
        this.editingCouponId = null;
        this.snackbar.open('Coupon updated', 'Close', { duration: 3000 });
      },
      error: (err) => {
        this.snackbar.open(err.error?.title || 'Error updating coupon', 'Close', { duration: 3000 });
      }
    });
  }

  toggleCoupon(id: number): void {
    this.adminService.toggleCoupon(id).subscribe({
      next: coupon => {
        this.coupons = this.coupons.map(c => c.id === coupon.id ? coupon : c);
      }
    });
  }
}
