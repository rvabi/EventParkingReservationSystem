using EventParking.Business.DTOs.Parking;

namespace EventParking.Business.Interfaces;

public interface IParkingSlotService
{
    Task<IReadOnlyList<ParkingSlotResponse>> GetByEventIdAsync(
        int eventId);

    Task<ParkingSlotResponse?> GetByIdAsync(
        int eventId,
        int parkingSlotId);

    Task<ParkingSlotResponse> CreateAsync(
        int eventId,
        CreateParkingSlotRequest request);

    Task<ParkingSlotResponse?> UpdateAsync(
        int eventId,
        int parkingSlotId,
        UpdateParkingSlotRequest request);

    Task<bool> DeleteAsync(
        int eventId,
        int parkingSlotId);
}
