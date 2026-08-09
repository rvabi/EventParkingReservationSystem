using EventParking.Business.DTOs.VenueFacilities;
using EventParking.Business.Interfaces;
using EventParking.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace EventParking.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FacilitiesController : ControllerBase
{
    private readonly IVenueFacilityService _facilityService;

    public FacilitiesController(
        IVenueFacilityService facilityService)
    {
        _facilityService = facilityService;
    }

    [HttpGet("~/api/venues/{venueId:int}/facilities")]
    public async Task<ActionResult<IEnumerable<VenueFacilityDto>>>
        GetByVenueId(int venueId)
    {
        var facilities =
            await _facilityService.GetByVenueIdAsync(venueId);

        var facilityDtos = facilities
            .Select(MapToDto)
            .ToList();

        return Ok(facilityDtos);
    }

    [HttpGet("{facilityId:int}")]
    public async Task<ActionResult<VenueFacilityDto>> GetById(
        int facilityId)
    {
        var facility =
            await _facilityService.GetByIdAsync(facilityId);

        if (facility is null)
        {
            return NotFound(new
            {
                message = "Facility not found."
            });
        }

        return Ok(MapToDto(facility));
    }

    [HttpPost("~/api/venues/{venueId:int}/facilities")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Create(
        int venueId,
        [FromBody] CreateVenueFacilityRequest request)
    {
        if (request.VenueId != venueId)
        {
            return BadRequest(new
            {
                message =
                    "VenueId in the request must match the route venueId."
            });
        }

        var facility = new VenueFacility
        {
            VenueId = venueId,
            Name = request.Name,
            FacilityType = request.FacilityType,
            Zone = request.Zone,
            Floor = request.Floor,
            Description = request.Description,
            IsAccessible = request.IsAccessible,
            Status = request.Status,
            Directions = request.Directions
        };

        var created =
            await _facilityService.CreateAsync(facility);

        if (!created)
        {
            return BadRequest(new
            {
                message =
                    "Unable to create facility. Check the venue and submitted data."
            });
        }

        return Ok(new
        {
            message = "Facility created successfully."
        });
    }

    [HttpPut("{facilityId:int}")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Update(
        int facilityId,
        [FromBody] UpdateVenueFacilityRequest request)
    {
        var existingFacility =
            await _facilityService.GetByIdAsync(facilityId);

        if (existingFacility is null)
        {
            return NotFound(new
            {
                message = "Facility not found."
            });
        }

        var facility = new VenueFacility
        {
            Id = facilityId,
            VenueId = request.VenueId,
            Name = request.Name,
            FacilityType = request.FacilityType,
            Zone = request.Zone,
            Floor = request.Floor,
            Description = request.Description,
            IsAccessible = request.IsAccessible,
            Status = request.Status,
            Directions = request.Directions
        };

        var updated =
            await _facilityService.UpdateAsync(facility);

        if (!updated)
        {
            return BadRequest(new
            {
                message =
                    "Unable to update facility. Check the venue and submitted data."
            });
        }

        var updatedFacility =
            await _facilityService.GetByIdAsync(facilityId);

        return Ok(new
        {
            message = "Facility updated successfully.",
            facility = updatedFacility is null
                ? null
                : MapToDto(updatedFacility)
        });
    }

    [HttpDelete("{facilityId:int}")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Delete(int facilityId)
    {
        var existingFacility =
            await _facilityService.GetByIdAsync(facilityId);

        if (existingFacility is null)
        {
            return NotFound(new
            {
                message = "Facility not found."
            });
        }

        var deleted =
            await _facilityService.DeleteAsync(facilityId);

        if (!deleted)
        {
            return BadRequest(new
            {
                message = "Unable to delete facility."
            });
        }

        return Ok(new
        {
            message = "Facility deleted successfully."
        });
    }

    private static VenueFacilityDto MapToDto(
        VenueFacility facility)
    {
        return new VenueFacilityDto
        {
            Id = facility.Id,
            VenueId = facility.VenueId,
            Name = facility.Name,
            FacilityType = facility.FacilityType.ToString(),
            Zone = facility.Zone,
            Floor = facility.Floor,
            Description = facility.Description,
            IsAccessible = facility.IsAccessible,
            Status = facility.Status.ToString(),
            Directions = facility.Directions,
            CreatedAt = facility.CreatedAt,
            UpdatedAt = facility.UpdatedAt
        };
    }
}