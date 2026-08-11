using EventParking.Business.DTOs.Events;
using EventParking.Business.Interfaces;
using EventParking.Models.Entities;
using EventParking.Models.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace EventParking.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly IEventService _eventService;

    public EventsController(IEventService eventService)
    {
        _eventService = eventService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<EventDto>>> GetAll(
        [FromQuery] string? name,
        [FromQuery] DateTime? date,
        [FromQuery] int? venueId,
        [FromQuery] int? categoryId)
    {
        var events = await _eventService.SearchAsync(
            name,
            date,
            venueId,
            categoryId);

        var eventDtos = events
            .Select(MapToDto)
            .ToList();

        return Ok(eventDtos);
    }

    [HttpGet("{eventId:int}")]
    public async Task<ActionResult<EventDto>> GetById(int eventId)
    {
        var eventEntity =
            await _eventService.GetByIdAsync(eventId);

        if (eventEntity is null)
        {
            return NotFound(new
            {
                message = "Event not found."
            });
        }

        return Ok(MapToDto(eventEntity));
    }

    [HttpPost]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Create(
        [FromBody] CreateEventRequest request)
    {
        var eventEntity = new Event
        {
            Name = request.Name,
            Description = request.Description,
            VenueId = request.VenueId,
            EventCategoryId = request.EventCategoryId,
            StartDateTime = request.StartDateTime,
            EndDateTime = request.EndDateTime,
            TicketPrice = request.TicketPrice,
            ParkingFee = request.ParkingFee,
            Capacity = request.Capacity,
            SeatingLayoutType =
                request.SeatingLayoutType ?? SeatingLayoutType.StraightRows
        };

        var created =
            await _eventService.CreateAsync(eventEntity);

        if (!created)
        {
            return BadRequest(new
            {
                message =
                    "Unable to create event. Check venue, category, schedule and capacity."
            });
        }

        return Ok(new
        {
            message = "Event created successfully."
        });
    }

    [HttpPut("{eventId:int}")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Update(
        int eventId,
        [FromBody] UpdateEventRequest request)
    {
        var existingEvent =
            await _eventService.GetByIdAsync(eventId);

        if (existingEvent is null)
        {
            return NotFound(new
            {
                message = "Event not found."
            });
        }

        var eventEntity = new Event
        {
            Id = eventId,
            Name = request.Name,
            Description = request.Description,
            VenueId = request.VenueId,
            EventCategoryId = request.EventCategoryId,
            StartDateTime = request.StartDateTime,
            EndDateTime = request.EndDateTime,
            TicketPrice = request.TicketPrice,
            ParkingFee = request.ParkingFee,
            Capacity = request.Capacity,
            SeatingLayoutType =
                request.SeatingLayoutType ?? existingEvent.SeatingLayoutType
        };

        var updated =
            await _eventService.UpdateAsync(eventEntity);

        if (!updated)
        {
            return BadRequest(new
            {
                message =
                    "Unable to update event. Check venue, category, schedule and capacity."
            });
        }

        var updatedEvent =
            await _eventService.GetByIdAsync(eventId);

        return Ok(new
        {
            message = "Event updated successfully.",
            eventData = updatedEvent is null
                ? null
                : MapToDto(updatedEvent)
        });
    }

    [HttpDelete("{eventId:int}")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Delete(int eventId)
    {
        var existingEvent =
            await _eventService.GetByIdAsync(eventId);

        if (existingEvent is null)
        {
            return NotFound(new
            {
                message = "Event not found."
            });
        }

        var deleted =
            await _eventService.DeleteAsync(eventId);

        if (!deleted)
        {
            return BadRequest(new
            {
                message = "Unable to delete event."
            });
        }

        return Ok(new
        {
            message = "Event deleted successfully."
        });
    }

    private static EventDto MapToDto(Event eventEntity)
    {
        return new EventDto
        {
            Id = eventEntity.Id,
            Name = eventEntity.Name,
            Description = eventEntity.Description,
            VenueId = eventEntity.VenueId,
            EventCategoryId = eventEntity.EventCategoryId,
            StartDateTime = eventEntity.StartDateTime,
            EndDateTime = eventEntity.EndDateTime,
            TicketPrice = eventEntity.TicketPrice,
            ParkingFee = eventEntity.ParkingFee,
            Capacity = eventEntity.Capacity,
            SeatingLayoutType = eventEntity.SeatingLayoutType.ToString(),
            CreatedAt = eventEntity.CreatedAt,
            UpdatedAt = eventEntity.UpdatedAt
        };
    }
}