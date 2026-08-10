using System.Collections.Generic;
using System.Threading.Tasks;
using EventParking.Business.DTOs.Seats;
using EventParking.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventParking.Api.Controllers;

[ApiController]
[Route("api/events/{eventId:int}/seats")]
public class SeatsController : ControllerBase
{
    private readonly ISeatService _seatService;

    public SeatsController(ISeatService seatService)
    {
        _seatService = seatService;
    }

    [HttpGet]
    public async Task<ActionResult<SeatMapResponseDto>> GetSeatMap(int eventId)
    {
        var seatMap = await _seatService.GetSeatMapAsync(eventId);
        if (seatMap is null)
        {
            return NotFound(new
            {
                message = "Event not found or seat map does not exist."
            });
        }

        return Ok(seatMap);
    }

    [HttpPost]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<SeatMapResponseDto>> GenerateSeatMap(
        int eventId,
        [FromBody] GenerateSeatMapRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var seatMap = await _seatService.GenerateSeatMapAsync(eventId, request);
        if (seatMap is null)
        {
            return BadRequest(new
            {
                message = "Unable to generate seat map. Verify event existence, seat capacity match, and that seats have not already been generated for this event."
            });
        }

        return CreatedAtAction(
            nameof(GetSeatMap),
            new { eventId = eventId },
            seatMap);
    }

    [HttpPut("row-price")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> UpdateRowPrice(
        int eventId,
        [FromQuery] string rowLabel,
        [FromBody] UpdateRowPriceRequest request)
    {
        if (string.IsNullOrWhiteSpace(rowLabel))
        {
            return BadRequest(new
            {
                message = "Row label is required."
            });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var updated = await _seatService.UpdateRowPriceAsync(eventId, rowLabel, request);
        if (!updated)
        {
            return BadRequest(new
            {
                message = "Unable to update row price. Ensure the row exists and contains no Held or Booked seats."
            });
        }

        return Ok(new
        {
            message = $"Row '{rowLabel}' price updated successfully."
        });
    }

    [HttpPost("validate")]
    public async Task<IActionResult> ValidateSeatSelection(
        int eventId,
        [FromBody] List<int> seatIds)
    {
        if (seatIds is null || seatIds.Count == 0)
        {
            return BadRequest(new
            {
                message = "Seat IDs list cannot be empty."
            });
        }

        var isValid = await _seatService.ValidateSeatSelectionAsync(eventId, seatIds);
        if (!isValid)
        {
            return BadRequest(new
            {
                message = "Seat selection invalid. Ensure all seats exist, belong to this event, and are currently Available."
            });
        }

        return Ok(new
        {
            message = "Seat selection is valid.",
            isValid = true
        });
    }

    [HttpPut("{seatId:int}")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> UpdateSeat(
        int eventId,
        int seatId,
        [FromBody] SeatDto seatDto)
    {
        if (seatDto is null)
        {
            return BadRequest(new
            {
                message = "Seat data is required."
            });
        }

        var existingSeat = await _seatService.GetByIdAsync(seatId);
        if (existingSeat is null)
        {
            return NotFound(new
            {
                message = "Seat not found."
            });
        }

        var updated = await _seatService.UpdateSeatAsync(eventId, seatId, seatDto);
        if (!updated)
        {
            return BadRequest(new
            {
                message = "Unable to update seat. Cannot renumber or modify Held or Booked seats."
            });
        }

        return Ok(new
        {
            message = "Seat updated successfully."
        });
    }

    [HttpDelete("{seatId:int}")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> DeleteSeat(
        int eventId,
        int seatId)
    {
        var existingSeat = await _seatService.GetByIdAsync(seatId);
        if (existingSeat is null)
        {
            return NotFound(new
            {
                message = "Seat not found."
            });
        }

        var deleted = await _seatService.DeleteSeatAsync(eventId, seatId);
        if (!deleted)
        {
            return BadRequest(new
            {
                message = "Unable to delete seat. Held or Booked seats cannot be deleted."
            });
        }

        return Ok(new
        {
            message = "Seat deleted successfully."
        });
    }
}
