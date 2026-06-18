using API.DTOs;
using Core.Entities;
using Core.Interfaces;
using Core.Specifications;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

public class CouponsController(IUnitOfWork unit) : BaseApiController
{
    [HttpGet("validate")]
    public async Task<ActionResult<CouponDto>> ValidateCoupon([FromQuery] string code)
    {
        var spec = new CouponSpecification(code.ToUpper());
        var coupon = await unit.Repository<Coupon>().GetEntityWithSpec(spec);

        if (coupon == null)
            return BadRequest(new ProblemDetails { Title = "Invalid coupon code" });

        if (!coupon.IsActive)
            return BadRequest(new ProblemDetails { Title = "This coupon is no longer active" });

        return Ok(new CouponDto
        {
            Id = coupon.Id,
            Code = coupon.Code,
            DiscountPercent = coupon.DiscountPercent,
            IsActive = coupon.IsActive
        });
    }
}
