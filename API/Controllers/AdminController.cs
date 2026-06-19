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
    // ─── Orders ──────────────────────────────────────────

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

    // ─── Products ────────────────────────────────────────

    [HttpGet("products")]
    public async Task<ActionResult<IReadOnlyList<Product>>> GetProducts()
    {
        var products = await unit.Repository<Product>().ListAllAsync();
        return Ok(products);
    }

    [InvalidateCache("api/products|")]
    [HttpPost("products")]
    public async Task<ActionResult<Product>> CreateProduct(CreateProductDto dto)
    {
        var product = new Product
        {
            Name = dto.Name,
            Description = dto.Description,
            Price = dto.Price,
            PictureUrl = dto.PictureUrl,
            Type = dto.Type,
            Brand = dto.Brand,
            QuantityInStock = dto.QuantityInStock
        };

        unit.Repository<Product>().Add(product);

        if (await unit.Complete())
        {
            return Ok(product);
        }

        return BadRequest("Problem creating product");
    }

    [InvalidateCache("api/products|")]
    [HttpPut("products/{id:int}")]
    public async Task<ActionResult> UpdateProduct(int id, UpdateProductDto dto)
    {
        var product = await unit.Repository<Product>().GetByIdAsync(id);
        if (product == null) return NotFound();

        product.Name = dto.Name;
        product.Description = dto.Description;
        product.Price = dto.Price;
        product.PictureUrl = dto.PictureUrl;
        product.Type = dto.Type;
        product.Brand = dto.Brand;
        product.QuantityInStock = dto.QuantityInStock;

        unit.Repository<Product>().Update(product);

        if (await unit.Complete()) return NoContent();

        return BadRequest("Problem updating product");
    }

    [InvalidateCache("api/products|")]
    [HttpDelete("products/{id:int}")]
    public async Task<ActionResult> DeleteProduct(int id)
    {
        var product = await unit.Repository<Product>().GetByIdAsync(id);
        if (product == null) return NotFound();

        unit.Repository<Product>().Remove(product);

        if (await unit.Complete()) return NoContent();

        return BadRequest("Problem deleting product");
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

    // ─── Brands ──────────────────────────────────────────

    [HttpGet("brands")]
    public async Task<ActionResult<IReadOnlyList<string>>> GetBrands()
    {
        var spec = new BrandListSpecification();
        return Ok(await unit.Repository<Product>().ListAsync(spec));
    }

    [HttpPost("brands")]
    public async Task<ActionResult> AddBrand(AddBrandDto dto)
    {
        var name = dto.Name.Trim();
        if (string.IsNullOrEmpty(name))
            return BadRequest("Brand name is required");

        var spec = new BrandListSpecification();
        var existing = await unit.Repository<Product>().ListAsync(spec);
        if (existing.Contains(name, StringComparer.OrdinalIgnoreCase))
            return BadRequest("Brand already exists");

        return NoContent();
    }

    [HttpDelete("brands/{name}")]
    public async Task<ActionResult> DeleteBrand(string name)
    {
        var spec = new ProductSpecification(new ProductSpecParams { Brands = [name] });
        var count = await unit.Repository<Product>().CountAsync(spec);

        if (count > 0)
            return BadRequest($"Cannot delete brand '{name}': {count} product(s) are using it");

        return NoContent();
    }

    // ─── Types ───────────────────────────────────────────

    [HttpGet("types")]
    public async Task<ActionResult<IReadOnlyList<string>>> GetTypes()
    {
        var spec = new TypeListSpecification();
        return Ok(await unit.Repository<Product>().ListAsync(spec));
    }

    [HttpPost("types")]
    public async Task<ActionResult> AddType(AddTypeDto dto)
    {
        var name = dto.Name.Trim();
        if (string.IsNullOrEmpty(name))
            return BadRequest("Type name is required");

        var spec = new TypeListSpecification();
        var existing = await unit.Repository<Product>().ListAsync(spec);
        if (existing.Contains(name, StringComparer.OrdinalIgnoreCase))
            return BadRequest("Type already exists");

        return NoContent();
    }

    [HttpDelete("types/{name}")]
    public async Task<ActionResult> DeleteType(string name)
    {
        var spec = new ProductSpecification(new ProductSpecParams { Types = [name] });
        var count = await unit.Repository<Product>().CountAsync(spec);

        if (count > 0)
            return BadRequest($"Cannot delete type '{name}': {count} product(s) are using it");

        return NoContent();
    }

    // ─── Image Upload ────────────────────────────────────

    [HttpPost("images/upload")]
    public async Task<ActionResult<ImageUploadResult>> UploadImage(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file provided");

        var allowedExtensions = new[] { ".png", ".jpg", ".jpeg", ".webp" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (!allowedExtensions.Contains(extension))
            return BadRequest("Invalid file type. Allowed: png, jpg, jpeg, webp");

        var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "products");
        Directory.CreateDirectory(uploadsDir);

        var fileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(uploadsDir, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return Ok(new ImageUploadResult
        {
            Url = $"/images/products/{fileName}"
        });
    }

    // ─── Coupons ─────────────────────────────────────────

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
