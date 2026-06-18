import { Component, inject } from '@angular/core';
import { CartService } from '../../core/services/cart.service';
import { CartItemComponent } from "./cart-item/cart-item.component";
import { OrderSummaryComponent } from "../../shared/components/order-summary/order-summary.component";
import { EmptyStateComponent } from "../../shared/components/empty-state/empty-state.component";
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatSnackBar } from '@angular/material/snack-bar';

@Component({
  selector: 'app-cart',
  imports: [CartItemComponent, OrderSummaryComponent, EmptyStateComponent, FormsModule, MatIconModule, MatButtonModule],
  templateUrl: './cart.component.html',
  styleUrl: './cart.component.scss'
})
export class CartComponent {
  private router = inject(Router);
  private snackbar = inject(MatSnackBar);
  cartService = inject(CartService);
  couponCode = '';
  applyingCoupon = false;

  onAction() {
    this.router.navigateByUrl('/shop');
  }

  applyCoupon() {
    if (!this.couponCode.trim()) return;
    this.applyingCoupon = true;
    this.cartService.applyCoupon(this.couponCode.trim()).subscribe({
      next: coupon => {
        const cart = this.cartService.cart();
        if (cart) {
          cart.couponCode = coupon.code;
          cart.discountPercent = coupon.discountPercent;
          this.cartService.setCart(cart);
          this.snackbar.open(`Coupon applied! ${coupon.discountPercent}% off`, 'Close', { duration: 3000 });
          this.couponCode = '';
        }
        this.applyingCoupon = false;
      },
      error: err => {
        this.snackbar.open(err.error?.title || 'Invalid coupon code', 'Close', { duration: 3000 });
        this.applyingCoupon = false;
      }
    });
  }

  removeCoupon() {
    this.cartService.removeCoupon();
    this.snackbar.open('Coupon removed', 'Close', { duration: 3000 });
  }
}