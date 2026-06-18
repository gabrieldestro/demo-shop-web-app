namespace API.DTOs;

public class CouponDto
{
    public int Id { get; set; }
    public required string Code { get; set; }
    public decimal DiscountPercent { get; set; }
    public bool IsActive { get; set; }
}

public class CreateCouponDto
{
    public required string Code { get; set; }
    public decimal DiscountPercent { get; set; }
}
