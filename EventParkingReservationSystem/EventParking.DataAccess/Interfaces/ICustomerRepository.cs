using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EventParking.Models.Entities;

namespace EventParking.DataAccess.Interfaces;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(int customerId);

    Task<Customer?> GetByEmailAsync(string email);

    Task<Customer?> GetByEmailVerificationTokenHashAsync(string tokenHash);

    Task<Customer?> GetByPasswordResetTokenHashAsync(string tokenHash);

    Task<IReadOnlyList<Customer>> GetAllAsync();

    Task<IReadOnlyList<Customer>> SearchAsync(string searchTerm);

    Task<bool> EmailExistsAsync(string email);

    Task<bool> HasActiveFutureBookingAsync(
        int customerId,
        DateTime currentDateTime);

    Task AddAsync(Customer customer);

    void Update(Customer customer);

    Task<int> SaveChangesAsync();
}
