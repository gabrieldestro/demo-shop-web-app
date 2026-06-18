import { Component, inject, Input } from '@angular/core';
import { Product } from '../../../shared/models/product';
import { MatIcon } from '@angular/material/icon';
import { CurrencyPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { CartService } from '../../../core/services/cart.service';

@Component({
  selector: 'app-product-item',
  imports: [
    MatIcon,
    CurrencyPipe,
    RouterLink
  ],
  templateUrl: './product-item.component.html',
  styleUrl: './product-item.component.scss'
})
export class ProductItemComponent {
 @Input() product?: Product;
 cartService = inject(CartService)

 cartQty(productId: number): number {
   return this.cartService.cart()?.items.find(i => i.productId === productId)?.quantity ?? 0;
 }

 canAddToCart(product: Product): boolean {
   return product.quantityInStock > 0 && this.cartQty(product.id) < product.quantityInStock;
 }
}