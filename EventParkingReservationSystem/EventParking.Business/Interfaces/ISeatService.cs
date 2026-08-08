using System.Collections.Generic;
using System.Threading.Tasks;
using EventParking.Business.DTOs.Seats;

namespace EventParking.Business.Interfaces;

public interface ISeatService
{
    Task<SeatMapResponseDto?> GetSeatMapAsync(int eventId);

    Task<SeatMapResponseDto?> GenerateSeatMapAsync(int eventId, GenerateSeatMapRequest request);

    Task<bool> UpdateRowPriceAsync(int eventId, string rowLabel, UpdateRowPriceRequest request);

    Task<bool> ValidateSeatSelectionAsync(int eventId, IEnumerable<int> seatIds);

    Task<SeatDto?> GetByIdAsync(int seatId);

    Task<bool> UpdateSeatAsync(int eventId, int seatId, SeatDto seatDto);

    Task<bool> DeleteSeatAsync(int eventId, int seatId);
}
