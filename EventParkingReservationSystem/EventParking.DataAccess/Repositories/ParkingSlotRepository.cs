using EventParking.DataAccess.Context;
using EventParking.DataAccess.Interfaces;
using EventParking.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventParking.DataAccess.Repositories;

public class ParkingSlotRepository : IParkingSlotRepository
{
    private readonly ApplicationDbContext _context;

    public ParkingSlotRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ParkingSlot?> GetByIdAsync(int parkingSlotId)
    {
        return await _context.ParkingSlots
            .Include(parkingSlot => parkingSlot.Event)
            .FirstOrDefaultAsync(
                parkingSlot => parkingSlot.Id == parkingSlotId);
    }

    public async Task<IReadOnlyList<ParkingSlot>> GetByEventIdAsync(
        int eventId)
    {
        return await _context.ParkingSlots
            .AsNoTracking()
            .Include(parkingSlot => parkingSlot.Event)
            .Where(parkingSlot => parkingSlot.EventId == eventId)
            .OrderBy(parkingSlot => parkingSlot.Zone)
            .ThenBy(parkingSlot => parkingSlot.SlotNumber)
            .ToListAsync();
    }

    public async Task<bool> SlotNumberExistsAsync(
        int eventId,
        string slotNumber,
        int? excludeParkingSlotId = null)
    {
        return await _context.ParkingSlots
            .AnyAsync(parkingSlot =>
                parkingSlot.EventId == eventId &&
                parkingSlot.SlotNumber == slotNumber &&
                (!excludeParkingSlotId.HasValue ||
                 parkingSlot.Id != excludeParkingSlotId.Value));
    }

    public async Task AddAsync(ParkingSlot parkingSlot)
    {
        await _context.ParkingSlots.AddAsync(parkingSlot);
    }

    public void Update(ParkingSlot parkingSlot)
    {
        _context.ParkingSlots.Update(parkingSlot);
    }

    public void Remove(ParkingSlot parkingSlot)
    {
        _context.ParkingSlots.Remove(parkingSlot);
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}