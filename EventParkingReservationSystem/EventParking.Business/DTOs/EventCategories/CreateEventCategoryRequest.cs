using System.ComponentModel.DataAnnotations;

namespace EventParking.Business.DTOs.EventCategories;

public class CreateEventCategoryRequest
{
    [Required]
    [StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }
}