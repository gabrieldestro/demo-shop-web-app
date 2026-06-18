using Core.Entities;

namespace Core.Specifications;

public class CouponSpecification : BaseSpecification<Core.Entities.Coupon>
{
    public CouponSpecification(string code)
        : base(c => c.Code == code)
    {
    }
}
