import { Component, inject } from '@angular/core';
import { CartService } from '../../../core/services/cart.service';
import { MatButton } from '@angular/material/button';
import { RouterLink } from '@angular/router';
import { CurrencyPipe, Location } from '@angular/common';

@Component({
  selector: 'app-order-summary',
  imports: [
    MatButton,
    RouterLink,
    CurrencyPipe
  ],
  templateUrl: './order-summary.component.html',
  styleUrl: './order-summary.component.scss'
})
export class OrderSummaryComponent {
  cartService = inject(CartService);
  location = inject(Location);
}