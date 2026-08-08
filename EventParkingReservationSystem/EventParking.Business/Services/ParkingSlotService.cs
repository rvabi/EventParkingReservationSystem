using EventParking.Business.DTOs.Parking;
using EventParking.Business.Interfaces;
using EventParking.DataAccess.Interfaces;
using EventParking.Models.Entities;
using EventParking.Models.Enums;

namespace EventParking.Business.Services;

public class ParkingSlotService : IParkingSlotService
{
    private readonly IParkingSlotRepository _parkingSlotRepository;

    public ParkingSlotService(
        IParkingSlotRepository parkingSlotRepository)
    {
        _parkingSlotRepository = parkingSlotRepository;
    }

    public async Task<IReadOnlyList<ParkingSlotResponse>>
        GetByEventIdAsync(int eventId)
    {
        if (eventId <= 0)
        {
            return Array.Empty<ParkingSlotResponse>();
        }

        var parkingSlots =
            await _parkingSlotRepository.GetByEventIdAsync(eventId);

        return parkingSlots
            .Select(MapToResponse)
            .ToList();
    }

    public async Task<ParkingSlotResponse?> GetByIdAsync(
        int eventId,
        int parkingSlotId)
    {
        if (eventId <= 0 || parkingSlotId <= 0)
        {
            return null;
        }

        var parkingSlot =
            await _parkingSlotRepository.GetByIdAsync(parkingSlotId);

        if (parkingSlot is null ||
            parkingSlot.EventId != eventId)
        {
            return null;
        }

        return MapToResponse(parkingSlot);
    }

    public async Task<ParkingSlotResponse> CreateAsync(
        int eventId,
        CreateParkingSlotRequest request)
    {
        if (eventId <= 0)
        {
            throw new ArgumentException(
                "A valid event ID is required.",
                nameof(eventId));
        }

        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var slotNumber = request.SlotNumber.Trim();

        if (string.IsNullOrWhiteSpace(slotNumber))
        {
            throw new ArgumentException(
                "Slot number is required.",
                nameof(request));
        }

        var slotNumberExists =
            await _parkingSlotRepository.SlotNumberExistsAsync(
                eventId,
                slotNumber);

        if (slotNumberExists)
        {
            throw new InvalidOperationException(
                $"Parking slot '{slotNumber}' already exists for this event.");
        }

        var parkingSlot = new ParkingSlot
        {
            EventId = eventId,
            SlotNumber = slotNumber,
            Zone = string.IsNullOrWhiteSpace(request.Zone)
                ? null
                : request.Zone.Trim(),
            FeeOverride = request.FeeOverride,
            Status = request.Status,
            CreatedAt = DateTime.UtcNow
        };

        await _parkingSlotRepository.AddAsync(parkingSlot);

        await _parkingSlotRepository.SaveChangesAsync();

        var createdParkingSlot =
            await _parkingSlotRepository.GetByIdAsync(parkingSlot.Id);

        if (createdParkingSlot is null)
        {
            throw new InvalidOperationException(
                "Parking slot was created but could not be retrieved.");
        }

        return MapToResponse(createdParkingSlot);
    }

    public async Task<ParkingSlotResponse?> UpdateAsync(
        int eventId,
        int parkingSlotId,
        UpdateParkingSlotRequest request)
    {
        if (eventId <= 0 || parkingSlotId <= 0)
        {
            return null;
        }

        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var parkingSlot =
            await _parkingSlotRepository.GetByIdAsync(parkingSlotId);

        if (parkingSlot is null ||
            parkingSlot.EventId != eventId)
        {
            return null;
        }

        if (parkingSlot.Status == ParkingSlotStatus.Held ||
            parkingSlot.Status == ParkingSlotStatus.Reserved)
        {
            throw new InvalidOperationException(
                "Held or reserved parking slots cannot be modified.");
        }

        var slotNumber = request.SlotNumber.Trim();

        if (string.IsNullOrWhiteSpace(slotNumber))
        {
            throw new ArgumentException(
                "Slot number is required.",
                nameof(request));
        }

        var slotNumberExists =
            await _parkingSlotRepository.SlotNumberExistsAsync(
                eventId,
                slotNumber,
                parkingSlotId);

        if (slotNumberExists)
        {
            throw new InvalidOperationException(
                $"Parking slot '{slotNumber}' already exists for this event.");
        }

        parkingSlot.SlotNumber = slotNumber;

        parkingSlot.Zone =
            string.IsNullOrWhiteSpace(request.Zone)
                ? null
                : request.Zone.Trim();

        parkingSlot.FeeOverride = request.FeeOverride;

        parkingSlot.Status = request.Status;

        parkingSlot.UpdatedAt = DateTime.UtcNow;

        _parkingSlotRepository.Update(parkingSlot);

        await _parkingSlotRepository.SaveChangesAsync();

        return MapToResponse(parkingSlot);
    }

    public async Task<bool> DeleteAsync(
        int eventId,
        int parkingSlotId)
    {
        if (eventId <= 0 || parkingSlotId <= 0)
        {
            return false;
        }

        var parkingSlot =
            await _parkingSlotRepository.GetByIdAsync(parkingSlotId);

        if (parkingSlot is null ||
            parkingSlot.EventId != eventId)
        {
            return false;
        }

        if (parkingSlot.Status == ParkingSlotStatus.Held ||
            parkingSlot.Status == ParkingSlotStatus.Reserved)
        {
            throw new InvalidOperationException(
                "Held or reserved parking slots cannot be deleted.");
        }

        _parkingSlotRepository.Remove(parkingSlot);

        await _parkingSlotRepository.SaveChangesAsync();

        return true;
    }

    private static ParkingSlotResponse MapToResponse(
        ParkingSlot parkingSlot)
    {
        var effectiveFee =
            parkingSlot.FeeOverride
            ?? parkingSlot.Event?.ParkingFee
            ?? 0m;

        return new ParkingSlotResponse
        {
            Id = parkingSlot.Id,
            EventId = parkingSlot.EventId,
            SlotNumber = parkingSlot.SlotNumber,
            Zone = parkingSlot.Zone,
            FeeOverride = parkingSlot.FeeOverride,
            EffectiveFee = effectiveFee,
            Status = parkingSlot.Status.ToString(),
            CreatedAt = parkingSlot.CreatedAt,
            UpdatedAt = parkingSlot.UpdatedAt
        };
    }
}
