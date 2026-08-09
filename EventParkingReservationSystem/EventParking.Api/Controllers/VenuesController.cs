using EventParking.Business.DTOs.Venues;
using EventParking.Business.Interfaces;
using EventParking.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace EventParking.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VenuesController : ControllerBase
{
    private readonly IVenueService _venueService;

    public VenuesController(IVenueService venueService)
    {
        _venueService = venueService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<VenueDto>>> GetAll()
    {
        var venues = await _venueService.GetAllAsync();

        var venueDtos = venues
            .Select(MapToDto)
            .ToList();

        return Ok(venueDtos);
    }


    [HttpGet("available")]
    public async Task<IActionResult> GetAvailableVenues(
    [FromQuery] DateTime startDateTime,
    [FromQuery] DateTime endDateTime,
    [FromQuery] int? venueId)
    {
        if (endDateTime <= startDateTime)
        {
            return BadRequest(new
            {
                message = "End date and time must be later than start date and time."
            });
        }

        if (venueId.HasValue && venueId.Value <= 0)
        {
            return BadRequest(new
            {
                message = "Venue ID must be greater than zero."
            });
        }

        var venues = await _venueService.GetAvailableVenuesAsync(
            startDateTime,
            endDateTime);

        if (venueId.HasValue)
        {
            venues = venues
                .Where(venue => venue.Id == venueId.Value)
                .ToList();
        }

        var response = venues
            .Select(MapToDto)
            .ToList();

        return Ok(response);
    }

    [HttpGet("{venueId:int}")]
    public async Task<ActionResult<VenueDto>> GetById(int venueId)
    {
        var venue =
            await _venueService.GetByIdAsync(venueId);

        if (venue is null)
        {
            return NotFound(new
            {
                message = "Venue not found."
            });
        }

        return Ok(MapToDto(venue));
    }



    [HttpPost]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Create(
        [FromBody] CreateVenueRequest request)
    {
        var venue = new Venue
        {
            Name = request.Name,
            Address = request.Address,
            TotalCapacity = request.TotalCapacity
        };

        var created =
            await _venueService.CreateAsync(venue);

        if (!created)
        {
            return BadRequest(new
            {
                message = "Unable to create venue. Check the submitted data."
            });
        }

        return Ok(new
        {
            message = "Venue created successfully."
        });
    }

    [HttpPut("{venueId:int}")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Update(
        int venueId,
        [FromBody] UpdateVenueRequest request)
    {
        var existingVenue =
            await _venueService.GetByIdAsync(venueId);

        if (existingVenue is null)
        {
            return NotFound(new
            {
                message = "Venue not found."
            });
        }

        var venue = new Venue
        {
            Id = venueId,
            Name = request.Name,
            Address = request.Address,
            TotalCapacity = request.TotalCapacity
        };

        var updated =
            await _venueService.UpdateAsync(venue);

        if (!updated)
        {
            return BadRequest(new
            {
                message = "Unable to update venue. Check the submitted data."
            });
        }

        var updatedVenue =
            await _venueService.GetByIdAsync(venueId);

        return Ok(new
        {
            message = "Venue updated successfully.",
            venue = updatedVenue is null
                ? null
                : MapToDto(updatedVenue)
        });
    }

    [HttpDelete("{venueId:int}")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Delete(int venueId)
    {
        var existingVenue =
            await _venueService.GetByIdAsync(venueId);

        if (existingVenue is null)
        {
            return NotFound(new
            {
                message = "Venue not found."
            });
        }

        var deleted =
            await _venueService.DeleteAsync(venueId);

        if (!deleted)
        {
            return BadRequest(new
            {
                message =
                    "Unable to delete venue. The venue may have upcoming events."
            });
        }

        return Ok(new
        {
            message = "Venue deleted successfully."
        });
    }

    private static VenueDto MapToDto(Venue venue)
    {
        return new VenueDto
        {
            Id = venue.Id,
            Name = venue.Name,
            Address = venue.Address,
            TotalCapacity = venue.TotalCapacity,
            CreatedAt = venue.CreatedAt,
            UpdatedAt = venue.UpdatedAt
        };
    }
}