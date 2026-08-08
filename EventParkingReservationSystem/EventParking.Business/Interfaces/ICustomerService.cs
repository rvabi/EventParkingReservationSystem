using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EventParking.Models.Entities;
using EventParking.Models.Enums;



using EventParking.Business.DTOs.Customers;


namespace EventParking.Business.Interfaces;

public interface ICustomerService
{
    Task<Customer?> GetByIdAsync(int customerId);

    Task<IReadOnlyList<Customer>> GetAllAsync();

    Task<IReadOnlyList<Customer>> SearchAsync(
        string searchTerm);

    Task<bool> UpdateProfileAsync(
        Customer customer);

    Task<CustomerStatusChangeResult> DeactivateAsync(
        int customerId);

    Task<CustomerStatusChangeResult> ReactivateAsync(
        int customerId);

    Task<bool> ChangeStatusAsync(
        int customerId,
        CustomerStatus status);
}
