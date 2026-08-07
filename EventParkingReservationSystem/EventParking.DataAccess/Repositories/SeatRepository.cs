using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EventParking.DataAccess.Context;
using EventParking.DataAccess.Interfaces;
using EventParking.Models.Entities;
using EventParking.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace EventParking.DataAccess.Repositories;

public class SeatRepository : ISeatRepository
{
    private readonly ApplicationDbContext _context;

    public SeatRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Seat?> GetByIdAsync(int seatId)
    {
        return await _context.Seats
            .Include(seat => seat.Event)
            .FirstOrDefaultAsync(seat => seat.Id == seatId);
    }

    public async Task<IReadOnlyList<Seat>> GetByEventIdAsync(int eventId)
    {
        return await _context.Seats
            .AsNoTracking()
            .Include(seat => seat.Event)
            .Where(seat => seat.EventId == eventId)
            .OrderBy(seat => seat.RowLabel)
            .ThenBy(seat => seat.ColumnNumber)
            .ThenBy(seat => seat.SeatNumber)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Seat>> GetByEventIdAndRowAsync(int eventId, string rowLabel)
    {
        return await _context.Seats
            .Where(seat => seat.EventId == eventId && seat.RowLabel == rowLabel)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Seat>> GetByEventIdAndIdsAsync(int eventId, IEnumerable<int> seatIds)
    {
        return await _context.Seats
            .AsNoTracking()
            .Include(seat => seat.Event)
            .Where(seat => seat.EventId == eventId && seatIds.Contains(seat.Id))
            .ToListAsync();
    }

    public async Task<bool> HasSeatsAsync(int eventId)
    {
        return await _context.Seats
            .AnyAsync(seat => seat.EventId == eventId);
    }

    public async Task<bool> RowHasHeldOrBookedSeatsAsync(int eventId, string rowLabel)
    {
        return await _context.Seats
            .AnyAsync(seat => seat.EventId == eventId &&
                              seat.RowLabel == rowLabel &&
                              (seat.Status == SeatStatus.Held || seat.Status == SeatStatus.Booked));
    }

    public async Task AddAsync(Seat seat)
    {
        await _context.Seats.AddAsync(seat);
    }

    public async Task AddRangeAsync(IEnumerable<Seat> seats)
    {
        await _context.Seats.AddRangeAsync(seats);
    }

    public void Update(Seat seat)
    {
        _context.Seats.Update(seat);
    }

    public void UpdateRange(IEnumerable<Seat> seats)
    {
        _context.Seats.UpdateRange(seats);
    }

    public void Remove(Seat seat)
    {
        _context.Seats.Remove(seat);
    }

    public void RemoveRange(IEnumerable<Seat> seats)
    {
        _context.Seats.RemoveRange(seats);
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}
