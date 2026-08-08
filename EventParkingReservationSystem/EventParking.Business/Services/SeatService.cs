using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using EventParking.Business.DTOs.Seats;
using EventParking.Business.Interfaces;
using EventParking.DataAccess.Interfaces;
using EventParking.Models.Entities;
using EventParking.Models.Enums;

namespace EventParking.Business.Services;

public class SeatService : ISeatService
{
    private readonly ISeatRepository _seatRepository;
    private readonly IEventRepository _eventRepository;

    public SeatService(
        ISeatRepository seatRepository,
        IEventRepository eventRepository)
    {
        _seatRepository = seatRepository;
        _eventRepository = eventRepository;
    }

    public async Task<SeatMapResponseDto?> GetSeatMapAsync(int eventId)
    {
        if (eventId <= 0)
        {
            return null;
        }

        var eventEntity = await _eventRepository.GetByIdAsync(eventId);
        if (eventEntity is null)
        {
            return null;
        }

        var seats = await _seatRepository.GetByEventIdAsync(eventId);

        var seatDtos = seats.Select(s => new SeatDto
        {
            Id = s.Id,
            SeatNumber = s.SeatNumber,
            RowLabel = s.RowLabel,
            ColumnNumber = s.ColumnNumber,
            Status = s.Status,
            Price = s.PriceOverride ?? eventEntity.TicketPrice
        }).ToList();

        return new SeatMapResponseDto
        {
            EventId = eventEntity.Id,
            Capacity = eventEntity.Capacity,
            TicketPrice = eventEntity.TicketPrice,
            TotalSeats = seatDtos.Count,
            Seats = seatDtos
        };
    }

    public async Task<SeatMapResponseDto?> GenerateSeatMapAsync(int eventId, GenerateSeatMapRequest request)
    {
        if (eventId <= 0 || request is null || request.Rows == null || !request.Rows.Any())
        {
            return null;
        }

        var eventEntity = await _eventRepository.GetByIdAsync(eventId);
        if (eventEntity is null || eventEntity.Capacity <= 0)
        {
            return null;
        }

        var hasExistingSeats = await _seatRepository.HasSeatsAsync(eventId);
        if (hasExistingSeats)
        {
            return null;
        }

        foreach (var rowReq in request.Rows)
        {
            if (rowReq.SeatCount < 1)
            {
                return null;
            }

            if (rowReq.RowPrice.HasValue && rowReq.RowPrice.Value < 0)
            {
                return null;
            }
        }

        int totalRequestedSeats = request.Rows.Sum(r => r.SeatCount);
        if (totalRequestedSeats != eventEntity.Capacity)
        {
            return null;
        }

        var seatsToCreate = new List<Seat>();
        int rowIndex = 0;

        foreach (var rowReq in request.Rows)
        {
            string rowLabel = GetRowLabel(rowIndex);

            for (int col = 1; col <= rowReq.SeatCount; col++)
            {
                var seat = new Seat
                {
                    EventId = eventId,
                    RowLabel = rowLabel,
                    ColumnNumber = col,
                    SeatNumber = $"{rowLabel}{col}",
                    PriceOverride = rowReq.RowPrice,
                    Status = SeatStatus.Available,
                    CreatedAt = DateTime.UtcNow
                };

                seatsToCreate.Add(seat);
            }

            rowIndex++;
        }

        await _seatRepository.AddRangeAsync(seatsToCreate);
        await _seatRepository.SaveChangesAsync();

        return await GetSeatMapAsync(eventId);
    }

    public async Task<bool> UpdateRowPriceAsync(int eventId, string rowLabel, UpdateRowPriceRequest request)
    {
        if (eventId <= 0 || string.IsNullOrWhiteSpace(rowLabel) || request is null)
        {
            return false;
        }

        if (request.RowPrice.HasValue && request.RowPrice.Value < 0)
        {
            return false;
        }

        var eventEntity = await _eventRepository.GetByIdAsync(eventId);
        if (eventEntity is null)
        {
            return false;
        }

        var rowLabelTrimmed = rowLabel.Trim();

        var isRowProtected = await _seatRepository.RowHasHeldOrBookedSeatsAsync(eventId, rowLabelTrimmed);
        if (isRowProtected)
        {
            return false;
        }

        var seats = await _seatRepository.GetByEventIdAndRowAsync(eventId, rowLabelTrimmed);
        if (seats is null || !seats.Any())
        {
            return false;
        }

        foreach (var seat in seats)
        {
            seat.PriceOverride = request.RowPrice;
            seat.UpdatedAt = DateTime.UtcNow;
        }

        _seatRepository.UpdateRange(seats);
        await _seatRepository.SaveChangesAsync();

        return true;
    }

    public async Task<bool> ValidateSeatSelectionAsync(int eventId, IEnumerable<int> seatIds)
    {
        if (eventId <= 0 || seatIds is null)
        {
            return false;
        }

        var seatIdsList = seatIds.ToList();
        if (!seatIdsList.Any())
        {
            return false;
        }

        if (seatIdsList.Count != seatIdsList.Distinct().Count())
        {
            return false;
        }

        var eventEntity = await _eventRepository.GetByIdAsync(eventId);
        if (eventEntity is null)
        {
            return false;
        }

        var seats = await _seatRepository.GetByEventIdAndIdsAsync(eventId, seatIdsList);

        if (seats.Count != seatIdsList.Count)
        {
            return false;
        }

        foreach (var seat in seats)
        {
            if (seat.EventId != eventId)
            {
                return false;
            }

            if (seat.Status != SeatStatus.Available)
            {
                return false;
            }
        }

        return true;
    }

    public async Task<SeatDto?> GetByIdAsync(int seatId)
    {
        if (seatId <= 0)
        {
            return null;
        }

        var seat = await _seatRepository.GetByIdAsync(seatId);
        if (seat is null)
        {
            return null;
        }

        decimal ticketPrice = seat.Event?.TicketPrice ?? 0m;

        return new SeatDto
        {
            Id = seat.Id,
            SeatNumber = seat.SeatNumber,
            RowLabel = seat.RowLabel,
            ColumnNumber = seat.ColumnNumber,
            Status = seat.Status,
            Price = seat.PriceOverride ?? ticketPrice
        };
    }

    public async Task<bool> UpdateSeatAsync(int eventId, int seatId, SeatDto seatDto)
    {
        if (eventId <= 0 || seatId <= 0 || seatDto is null)
        {
            return false;
        }

        var existingSeat = await _seatRepository.GetByIdAsync(seatId);
        if (existingSeat is null || existingSeat.EventId != eventId)
        {
            return false;
        }

        bool isRenumbering = !string.Equals(existingSeat.SeatNumber, seatDto.SeatNumber, StringComparison.OrdinalIgnoreCase) ||
                             !string.Equals(existingSeat.RowLabel, seatDto.RowLabel, StringComparison.OrdinalIgnoreCase) ||
                             existingSeat.ColumnNumber != seatDto.ColumnNumber;

        if ((existingSeat.Status == SeatStatus.Held || existingSeat.Status == SeatStatus.Booked) && isRenumbering)
        {
            return false;
        }

        existingSeat.RowLabel = seatDto.RowLabel;
        existingSeat.ColumnNumber = seatDto.ColumnNumber;
        existingSeat.SeatNumber = string.IsNullOrWhiteSpace(seatDto.SeatNumber) ? $"{seatDto.RowLabel}{seatDto.ColumnNumber}" : seatDto.SeatNumber.Trim();
        existingSeat.Status = seatDto.Status;
        existingSeat.UpdatedAt = DateTime.UtcNow;

        _seatRepository.Update(existingSeat);
        await _seatRepository.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteSeatAsync(int eventId, int seatId)
    {
        if (eventId <= 0 || seatId <= 0)
        {
            return false;
        }

        var seat = await _seatRepository.GetByIdAsync(seatId);
        if (seat is null || seat.EventId != eventId)
        {
            return false;
        }

        if (seat.Status == SeatStatus.Held || seat.Status == SeatStatus.Booked)
        {
            return false;
        }

        _seatRepository.Remove(seat);
        await _seatRepository.SaveChangesAsync();

        return true;
    }

    private static string GetRowLabel(int index)
    {
        string result = string.Empty;
        index++;
        while (index > 0)
        {
            int rem = (index - 1) % 26;
            result = (char)('A' + rem) + result;
            index = (index - 1) / 26;
        }
        return result;
    }
}
