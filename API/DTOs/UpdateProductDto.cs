using System.ComponentModel.DataAnnotations;

namespace API.DTOs;

public class UpdateProductDto
{
    [Required] public string Name { get; set; } = string.Empty;

    [Required] public string Description { get; set; } = string.Empty;

    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
    [Required] public decimal Price { get; set; }

    [Required] public string PictureUrl { get; set; } = string.Empty;
    [Required] public string Type { get; set; } = string.Empty;
    [Required] public string Brand { get; set; } = string.Empty;

    [Range(0, int.MaxValue, ErrorMessage = "QuantityInStock must be 0 or more")]
    [Required] public int QuantityInStock { get; set; }
}
