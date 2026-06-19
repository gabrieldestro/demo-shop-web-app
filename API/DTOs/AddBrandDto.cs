using System.ComponentModel.DataAnnotations;

namespace API.DTOs;

public class AddBrandDto
{
    [Required] public string Name { get; set; } = string.Empty;
}
