using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EventParking.Models.Entities;

namespace EventParking.DataAccess.Interfaces;

public interface ISeatRepository
{
    Task<Seat?> GetByIdAsync(int seatId);

    Task<IReadOnlyList<Seat>> GetByEventIdAsync(int eventId);

    Task<IReadOnlyList<Seat>> GetByEventIdAndRowAsync(int eventId, string rowLabel);

    Task<IReadOnlyList<Seat>> GetByEventIdAndIdsAsync(int eventId, IEnumerable<int> seatIds);

    Task<bool> HasSeatsAsync(int eventId);

    Task<bool> RowHasHeldOrBookedSeatsAsync(int eventId, string rowLabel);

    Task AddAsync(Seat seat);

    Task AddRangeAsync(IEnumerable<Seat> seats);

    void Update(Seat seat);

    void UpdateRange(IEnumerable<Seat> seats);

    void Remove(Seat seat);

    void RemoveRange(IEnumerable<Seat> seats);

    Task<int> SaveChangesAsync();
}
