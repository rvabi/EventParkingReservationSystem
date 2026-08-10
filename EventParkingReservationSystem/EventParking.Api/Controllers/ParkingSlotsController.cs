using EventParking.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using EventParking.Business.DTOs.Parking;
using EventParking.Business.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EventParking.Api.Controllers;

[ApiController]
[Route("api/events/{eventId:int}/parking-slots")]
public class ParkingSlotsController : ControllerBase
{
    private readonly IParkingSlotService _parkingSlotService;

    public ParkingSlotsController(
        IParkingSlotService parkingSlotService)
    {
        _parkingSlotService = parkingSlotService;
    }

    // GET: /api/events/{eventId}/parking-slots
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ParkingSlotResponse>>>
        GetByEventId(int eventId)
    {
        if (eventId <= 0)
        {
            return BadRequest(new
            {
                message = "A valid event ID is required."
            });
        }

        var parkingSlots =
            await _parkingSlotService.GetByEventIdAsync(eventId);

        return Ok(parkingSlots);
    }

    // GET: /api/events/{eventId}/parking-slots/{parkingSlotId}
    [HttpGet("{parkingSlotId:int}")]
    public async Task<ActionResult<ParkingSlotResponse>> GetById(
        int eventId,
        int parkingSlotId)
    {
        if (eventId <= 0 || parkingSlotId <= 0)
        {
            return BadRequest(new
            {
                message =
                    "A valid event ID and parking slot ID are required."
            });
        }

        var parkingSlot =
            await _parkingSlotService.GetByIdAsync(
                eventId,
                parkingSlotId);

        if (parkingSlot is null)
        {
            return NotFound(new
            {
                message = "Parking slot not found."
            });
        }

        return Ok(parkingSlot);
    }

    // POST: /api/events/{eventId}/parking-slots
    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Administrator))]
    public async Task<ActionResult<ParkingSlotResponse>> Create(
        int eventId,
        [FromBody] CreateParkingSlotRequest request)
    {
        if (eventId <= 0)
        {
            return BadRequest(new
            {
                message = "A valid event ID is required."
            });
        }

        try
        {
            var createdParkingSlot =
                await _parkingSlotService.CreateAsync(
                    eventId,
                    request);

            return CreatedAtAction(
                nameof(GetById),
                new
                {
                    eventId,
                    parkingSlotId = createdParkingSlot.Id
                },
                createdParkingSlot);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new
            {
                message = exception.Message
            });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new
            {
                message = exception.Message
            });
        }
    }

    // PUT: /api/events/{eventId}/parking-slots/{parkingSlotId}
    [HttpPut("{parkingSlotId:int}")]
    [Authorize(Roles = nameof(UserRole.Administrator))]
    public async Task<ActionResult<ParkingSlotResponse>> Update(
        int eventId,
        int parkingSlotId,
        [FromBody] UpdateParkingSlotRequest request)
    {
        if (eventId <= 0 || parkingSlotId <= 0)
        {
            return BadRequest(new
            {
                message =
                    "A valid event ID and parking slot ID are required."
            });
        }

        try
        {
            var updatedParkingSlot =
                await _parkingSlotService.UpdateAsync(
                    eventId,
                    parkingSlotId,
                    request);

            if (updatedParkingSlot is null)
            {
                return NotFound(new
                {
                    message = "Parking slot not found."
                });
            }

            return Ok(updatedParkingSlot);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new
            {
                message = exception.Message
            });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new
            {
                message = exception.Message
            });
        }
    }

    // DELETE: /api/events/{eventId}/parking-slots/{parkingSlotId}
    [HttpDelete("{parkingSlotId:int}")]
    [Authorize(Roles = nameof(UserRole.Administrator))]
    public async Task<IActionResult> Delete(
        int eventId,
        int parkingSlotId)
    {
        if (eventId <= 0 || parkingSlotId <= 0)
        {
            return BadRequest(new
            {
                message =
                    "A valid event ID and parking slot ID are required."
            });
        }

        try
        {
            var deleted =
                await _parkingSlotService.DeleteAsync(
                    eventId,
                    parkingSlotId);

            if (!deleted)
            {
                return NotFound(new
                {
                    message = "Parking slot not found."
                });
            }

            return NoContent();
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new
            {
                message = exception.Message
            });
        }
    }
}