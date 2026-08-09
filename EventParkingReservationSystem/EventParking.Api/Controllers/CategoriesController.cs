using EventParking.Business.DTOs.EventCategories;
using EventParking.Business.Interfaces;
using EventParking.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace EventParking.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly IEventCategoryService _categoryService;

    public CategoriesController(
        IEventCategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<EventCategoryDto>>> GetAll()
    {
        var categories =
            await _categoryService.GetAllAsync();

        var categoryDtos = categories
            .Select(MapToDto)
            .ToList();

        return Ok(categoryDtos);
    }

    [HttpGet("{categoryId:int}")]
    public async Task<ActionResult<EventCategoryDto>> GetById(
        int categoryId)
    {
        var category =
            await _categoryService.GetByIdAsync(categoryId);

        if (category is null)
        {
            return NotFound(new
            {
                message = "Event category not found."
            });
        }

        return Ok(MapToDto(category));
    }

    [HttpPost]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Create(
        [FromBody] CreateEventCategoryRequest request)
    {
        var category = new EventCategory
        {
            Name = request.Name,
            Description = request.Description
        };

        var created =
            await _categoryService.CreateAsync(category);

        if (!created)
        {
            return BadRequest(new
            {
                message =
                    "Unable to create event category. Check the submitted data."
            });
        }

        return Ok(new
        {
            message = "Event category created successfully."
        });
    }

    [HttpPut("{categoryId:int}")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Update(
        int categoryId,
        [FromBody] UpdateEventCategoryRequest request)
    {
        var existingCategory =
            await _categoryService.GetByIdAsync(categoryId);

        if (existingCategory is null)
        {
            return NotFound(new
            {
                message = "Event category not found."
            });
        }

        var category = new EventCategory
        {
            Id = categoryId,
            Name = request.Name,
            Description = request.Description
        };

        var updated =
            await _categoryService.UpdateAsync(category);

        if (!updated)
        {
            return BadRequest(new
            {
                message =
                    "Unable to update event category. Check the submitted data."
            });
        }

        var updatedCategory =
            await _categoryService.GetByIdAsync(categoryId);

        return Ok(new
        {
            message = "Event category updated successfully.",
            category = updatedCategory is null
                ? null
                : MapToDto(updatedCategory)
        });
    }

    [HttpDelete("{categoryId:int}")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Delete(int categoryId)
    {
        var existingCategory =
            await _categoryService.GetByIdAsync(categoryId);

        if (existingCategory is null)
        {
            return NotFound(new
            {
                message = "Event category not found."
            });
        }

        var deleted =
            await _categoryService.DeleteAsync(categoryId);

        if (!deleted)
        {
            return BadRequest(new
            {
                message =
                    "Unable to delete event category. The category may be assigned to an existing event."
            });
        }

        return Ok(new
        {
            message = "Event category deleted successfully."
        });
    }

    private static EventCategoryDto MapToDto(
        EventCategory category)
    {
        return new EventCategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            CreatedAt = category.CreatedAt,
            UpdatedAt = category.UpdatedAt
        };
    }
}