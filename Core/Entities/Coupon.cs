namespace Core.Entities;

public class Coupon : BaseEntity
{
    public required string Code { get; set; }
    public decimal DiscountPercent { get; set; }
    public bool IsActive { get; set; } = true;
}
