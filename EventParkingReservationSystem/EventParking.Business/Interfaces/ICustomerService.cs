using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EventParking.Models.Entities;
using EventParking.Models.Enums;

namespace EventParking.Business.Interfaces;

public interface ICustomerService
{
    Task<Customer?> GetByIdAsync(int customerId);

    Task<IReadOnlyList<Customer>> GetAllAsync();

    Task<bool> UpdateProfileAsync(Customer customer);

    Task<bool> ChangeStatusAsync(
        int customerId,
        CustomerStatus status);
}
