using System;
using API.DTOs;
using API.Extensions;
using API.RequestHelpers;
using Core.Entities;
using Core.Entities.OrderAggregate;
using Core.Interfaces;
using Core.Specifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController(IUnitOfWork unit, IPaymentService paymentService) : BaseApiController
{
    [HttpGet("orders")]
    public async Task<ActionResult<IReadOnlyList<OrderDto>>> GetOrders([FromQuery] OrderSpecParams specParams)
    {
        var spec = new OrderSpecification(specParams);

        return await CreatePagedResult(unit.Repository<Order>(),
            spec, specParams.PageIndex, specParams.PageSize, o => o.ToDto());
    }

    [HttpGet("orders/{id:int}")]
    public async Task<ActionResult<OrderDto>> GetOrderById(int id)
    {
        var spec = new OrderSpecification(id);

        var order = await unit.Repository<Order>().GetEntityWithSpec(spec);

        if (order == null) return BadRequest("No order with that Id");

        return order.ToDto();
    }

    [HttpPost("orders/refund/{id:int}")]
    public async Task<ActionResult<OrderDto>> RefundOrder(int id)
    {
        var spec = new OrderSpecification(id);

        var order = await unit.Repository<Order>().GetEntityWithSpec(spec);

        if (order == null) return BadRequest("No order with that Id");

        if (order.Status == OrderStatus.Pending)
            return BadRequest("Payment not received for this order");

        var result = await paymentService.RefundPayment(order.PaymentIntentId);

        if (result == "succeeded")
        {
            order.Status = OrderStatus.Refunded;

            await unit.Complete();

            return order.ToDto();
        }

        return BadRequest("Problem refunding order");
    }

    [HttpGet("products")]
    public async Task<ActionResult<IReadOnlyList<Product>>> GetProducts()
    {
        var products = await unit.Repository<Product>().ListAllAsync();
        return Ok(products);
    }

    [InvalidateCache("api/products|")]
    [HttpPut("products/{id:int}/stock")]
    public async Task<ActionResult> UpdateProductStock(int id, UpdateStockDto dto)
    {
        var product = await unit.Repository<Product>().GetByIdAsync(id);
        if (product == null) return NotFound();

        product.QuantityInStock = dto.QuantityInStock;
        unit.Repository<Product>().Update(product);

        if (await unit.Complete()) return NoContent();

        return BadRequest("Problem updating stock");
    }

    [HttpGet("coupons")]
    public async Task<ActionResult<IReadOnlyList<CouponDto>>> GetCoupons()
    {
        var coupons = await unit.Repository<Coupon>().ListAllAsync();
        return Ok(coupons.Select(c => new CouponDto
        {
            Id = c.Id,
            Code = c.Code,
            DiscountPercent = c.DiscountPercent,
            IsActive = c.IsActive
        }).ToList());
    }

    [HttpPost("coupons")]
    public async Task<ActionResult<CouponDto>> CreateCoupon(CreateCouponDto dto)
    {
        var spec = new CouponSpecification(dto.Code.ToUpper());
        var existing = await unit.Repository<Coupon>().GetEntityWithSpec(spec);
        if (existing != null)
            return BadRequest("A coupon with this code already exists");

        var coupon = new Coupon
        {
            Code = dto.Code.ToUpper(),
            DiscountPercent = dto.DiscountPercent,
            IsActive = true
        };

        unit.Repository<Coupon>().Add(coupon);
        if (await unit.Complete())
        {
            return new CouponDto
            {
                Id = coupon.Id,
                Code = coupon.Code,
                DiscountPercent = coupon.DiscountPercent,
                IsActive = coupon.IsActive
            };
        }

        return BadRequest("Problem creating coupon");
    }

    [HttpPut("coupons/{id:int}")]
    public async Task<ActionResult<CouponDto>> UpdateCoupon(int id, CreateCouponDto dto)
    {
        var coupon = await unit.Repository<Coupon>().GetByIdAsync(id);
        if (coupon == null) return NotFound();

        coupon.Code = dto.Code.ToUpper();
        coupon.DiscountPercent = dto.DiscountPercent;

        unit.Repository<Coupon>().Update(coupon);
        if (await unit.Complete())
        {
            return new CouponDto
            {
                Id = coupon.Id,
                Code = coupon.Code,
                DiscountPercent = coupon.DiscountPercent,
                IsActive = coupon.IsActive
            };
        }

        return BadRequest("Problem updating coupon");
    }

    [HttpPut("coupons/{id:int}/toggle")]
    public async Task<ActionResult<CouponDto>> ToggleCoupon(int id)
    {
        var coupon = await unit.Repository<Coupon>().GetByIdAsync(id);
        if (coupon == null) return NotFound();

        coupon.IsActive = !coupon.IsActive;

        unit.Repository<Coupon>().Update(coupon);
        if (await unit.Complete())
        {
            return new CouponDto
            {
                Id = coupon.Id,
                Code = coupon.Code,
                DiscountPercent = coupon.DiscountPercent,
                IsActive = coupon.IsActive
            };
        }

        return BadRequest("Problem toggling coupon");
    }
}