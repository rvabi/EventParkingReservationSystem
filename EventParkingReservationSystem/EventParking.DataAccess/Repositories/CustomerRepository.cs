using EventParking.DataAccess.Context;
using EventParking.DataAccess.Interfaces;
using EventParking.Models.Entities;
using EventParking.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace EventParking.DataAccess.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly ApplicationDbContext _context;

    public CustomerRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Customer?> GetByIdAsync(int customerId)
    {
        return await _context.Customers
            .FirstOrDefaultAsync(customer => customer.Id == customerId);
    }

    public async Task<Customer?> GetByEmailAsync(string email)
    {
        string normalizedEmail = email.Trim().ToLowerInvariant();

        return await _context.Customers
            .FirstOrDefaultAsync(
                customer => customer.Email.ToLower() == normalizedEmail);
    }

    public async Task<Customer?> GetByEmailVerificationTokenHashAsync(
        string tokenHash)
    {
        return await _context.Customers
            .FirstOrDefaultAsync(
                customer =>
                    customer.EmailVerificationTokenHash == tokenHash);
    }

    public async Task<Customer?> GetByPasswordResetTokenHashAsync(
        string tokenHash)
    {
        return await _context.Customers
            .FirstOrDefaultAsync(
                customer =>
                    customer.PasswordResetTokenHash == tokenHash);
    }

    public async Task<IReadOnlyList<Customer>> GetAllAsync()
    {
        return await _context.Customers
            .AsNoTracking()
            .OrderBy(customer => customer.FullName)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Customer>> SearchAsync(
        string searchTerm)
    {
        string normalizedSearchTerm =
            searchTerm.Trim().ToLowerInvariant();

        return await _context.Customers
            .AsNoTracking()
            .Where(customer =>
                customer.Role == UserRole.Customer &&
                (
                    customer.FullName.ToLower()
                        .Contains(normalizedSearchTerm) ||
                    customer.Email.ToLower()
                        .Contains(normalizedSearchTerm) ||
                    customer.Phone.ToLower()
                        .Contains(normalizedSearchTerm)
                ))
            .OrderBy(customer => customer.FullName)
            .ToListAsync();
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        string normalizedEmail = email.Trim().ToLowerInvariant();

        return await _context.Customers
            .AnyAsync(
                customer =>
                    customer.Email.ToLower() == normalizedEmail);
    }

    public async Task<bool> HasActiveFutureBookingAsync(
        int customerId,
        DateTime currentDateTime)
    {
        return await _context.Bookings
            .AnyAsync(booking =>
                booking.CustomerId == customerId &&
                booking.Event.StartDateTime > currentDateTime &&
                (
                    booking.Status == BookingStatus.Confirmed ||
                    (
                        booking.Status == BookingStatus.Pending &&
                        (
                            booking.HoldExpiresAt == null ||
                            booking.HoldExpiresAt > currentDateTime
                        )
                    )
                ));
    }

    public async Task AddAsync(Customer customer)
    {
        await _context.Customers.AddAsync(customer);
    }

    public void Update(Customer customer)
    {
        _context.Customers.Update(customer);
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}