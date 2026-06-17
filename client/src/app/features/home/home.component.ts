import { Component, inject, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ShopService } from '../../core/services/shop.service';
import { Product } from '../../shared/models/product';
import { ShopParams } from '../../shared/models/shopParams';
import { ProductItemComponent } from '../shop/product-item/product-item.component';
import { MatIcon } from '@angular/material/icon';

@Component({
  selector: 'app-home',
  imports: [
    RouterLink,
    ProductItemComponent,
    MatIcon
  ],
  templateUrl: './home.component.html',
  styleUrl: './home.component.scss'
})
export class HomeComponent implements OnInit {
  private shopService = inject(ShopService);
  featuredProducts: Product[] = [];

  ngOnInit() {
    const params = new ShopParams();
    params.pageSize = 3;
    this.shopService.getProducts(params).subscribe({
      next: res => this.featuredProducts = res.data
    });
  }
}