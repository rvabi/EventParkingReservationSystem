using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EventParking.Business.Interfaces;
using EventParking.DataAccess.Interfaces;
using EventParking.Models.Entities;
using EventParking.Models.Enums;

namespace EventParking.Business.Services;

public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _customerRepository;

    public CustomerService(
        ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task<Customer?> GetByIdAsync(int customerId)
    {
        if (customerId <= 0)
        {
            return null;
        }

        return await _customerRepository.GetByIdAsync(customerId);
    }

    public async Task<IReadOnlyList<Customer>> GetAllAsync()
    {
        return await _customerRepository.GetAllAsync();
    }

    public async Task<bool> UpdateProfileAsync(Customer customer)
    {
        if (customer.Id <= 0)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(customer.FullName) ||
            string.IsNullOrWhiteSpace(customer.Email) ||
            string.IsNullOrWhiteSpace(customer.Phone))
        {
            return false;
        }

        var existingCustomer =
            await _customerRepository.GetByIdAsync(customer.Id);

        if (existingCustomer is null)
        {
            return false;
        }

        var normalizedEmail =
            customer.Email.Trim().ToLowerInvariant();

        if (!string.Equals(
                existingCustomer.Email,
                normalizedEmail,
                StringComparison.OrdinalIgnoreCase))
        {
            var emailExists =
                await _customerRepository.EmailExistsAsync(
                    normalizedEmail);

            if (emailExists)
            {
                return false;
            }
        }

        existingCustomer.FullName = customer.FullName.Trim();
        existingCustomer.Email = normalizedEmail;
        existingCustomer.Phone = customer.Phone.Trim();
        existingCustomer.UpdatedAt = DateTime.UtcNow;

        _customerRepository.Update(existingCustomer);

        await _customerRepository.SaveChangesAsync();

        return true;
    }

    public async Task<bool> ChangeStatusAsync(
        int customerId,
        CustomerStatus status)
    {
        if (customerId <= 0)
        {
            return false;
        }

        var customer =
            await _customerRepository.GetByIdAsync(customerId);

        if (customer is null)
        {
            return false;
        }

        customer.Status = status;
        customer.UpdatedAt = DateTime.UtcNow;

        _customerRepository.Update(customer);

        await _customerRepository.SaveChangesAsync();

        return true;
    }
}
