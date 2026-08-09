using EventParking.Models.Entities;

namespace EventParking.DataAccess.Interfaces;

public interface IParkingSlotRepository
{
    Task<ParkingSlot?> GetByIdAsync(int parkingSlotId);

    Task<IReadOnlyList<ParkingSlot>> GetByEventIdAsync(int eventId);

    Task<bool> SlotNumberExistsAsync(
        int eventId,
        string slotNumber,
        int? excludeParkingSlotId = null);

    Task AddAsync(ParkingSlot parkingSlot);

    void Update(ParkingSlot parkingSlot);

    void Remove(ParkingSlot parkingSlot);

    Task<int> SaveChangesAsync();
}
