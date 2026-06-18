import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import { HttpClient, HttpParams } from '@angular/common/http';
import { OrderParams } from '../../shared/models/orderParams';
import { Order } from '../../shared/models/order';
import { Pagination } from '../../shared/models/pagination';
import { Product } from '../../shared/models/product';

@Injectable({
  providedIn: 'root'
})
export class AdminService {
  baseUrl = environment.baseUrl;
  private http = inject(HttpClient);

  getOrders(orderParams: OrderParams) {
    let params = new HttpParams();
    if (orderParams.filter && orderParams.filter !== 'All') {
      params = params.append('status', orderParams.filter);
    } 
    params = params.append('pageSize', orderParams.pageSize);
    params = params.append('pageIndex', orderParams.pageNumber);
    return this.http.get<Pagination<Order>>(this.baseUrl + 'admin/orders', {params});
  }

  getOrder(id: number) {
    return this.http.get<Order>(this.baseUrl + 'admin/orders/' + id);
  }

  refundOrder(id: number) {
    return this.http.post<Order>(this.baseUrl + 'admin/orders/refund/' + id, {});
  }

  getProducts() {
    return this.http.get<Product[]>(this.baseUrl + 'admin/products');
  }

  updateProductStock(id: number, quantityInStock: number) {
    return this.http.put(this.baseUrl + 'admin/products/' + id + '/stock', { quantityInStock });
  }

  getCoupons() {
    return this.http.get<CouponDto[]>(this.baseUrl + 'admin/coupons');
  }

  createCoupon(code: string, discountPercent: number) {
    return this.http.post<CouponDto>(this.baseUrl + 'admin/coupons', { code, discountPercent });
  }

  updateCoupon(id: number, code: string, discountPercent: number) {
    return this.http.put<CouponDto>(this.baseUrl + 'admin/coupons/' + id, { code, discountPercent });
  }

  toggleCoupon(id: number) {
    return this.http.put<CouponDto>(this.baseUrl + 'admin/coupons/' + id + '/toggle', {});
  }
}

export type CouponDto = {
  id: number;
  code: string;
  discountPercent: number;
  isActive: boolean;
}