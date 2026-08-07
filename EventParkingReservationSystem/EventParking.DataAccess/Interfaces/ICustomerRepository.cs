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

    Task<IReadOnlyList<Customer>> GetAllAsync();

    Task<bool> EmailExistsAsync(string email);

    Task AddAsync(Customer customer);

    void Update(Customer customer);

    Task<int> SaveChangesAsync();
}
