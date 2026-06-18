using System.ComponentModel.DataAnnotations;

namespace API.DTOs;

public class UpdateStockDto
{
    [Range(0, int.MaxValue, ErrorMessage = "Quantity in stock must be 0 or more")]
    [Required]
    public int QuantityInStock { get; set; }
}
