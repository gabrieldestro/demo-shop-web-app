using System.ComponentModel.DataAnnotations;

namespace API.DTOs;

public class AddTypeDto
{
    [Required] public string Name { get; set; } = string.Empty;
}
