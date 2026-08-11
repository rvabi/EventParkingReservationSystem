using EventParking.Business.DTOs.Customers;
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

    public async Task<IReadOnlyList<Customer>> SearchAsync(
    string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return await _customerRepository.GetAllAsync();
        }

        return await _customerRepository.SearchAsync(
            searchTerm.Trim());
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
        if (status == CustomerStatus.Deactivated)
        {
            var result =
                await DeactivateAsync(customerId);

            return result.Success;
        }

        if (status == CustomerStatus.Active)
        {
            var result =
                await ReactivateAsync(customerId);

            return result.Success;
        }

        return false;
    }

    public async Task<CustomerStatusChangeResult> DeactivateAsync(
    int customerId)
    {
        var customer =
            await _customerRepository.GetByIdAsync(customerId);

        if (customer is null)
        {
            return new CustomerStatusChangeResult
            {
                Success = false,
                ErrorCode = "CUSTOMER_NOT_FOUND",
                Message = "Customer not found."
            };
        }

        if (customer.Status == CustomerStatus.Deactivated)
        {
            return new CustomerStatusChangeResult
            {
                Success = true,
                Message = "Customer account is already deactivated."
            };
        }

        bool hasActiveFutureBooking =
            await _customerRepository.HasActiveFutureBookingAsync(
                customerId,
                DateTime.UtcNow);

        if (hasActiveFutureBooking)
        {
            return new CustomerStatusChangeResult
            {
                Success = false,
                ErrorCode = "ACTIVE_FUTURE_BOOKING",
                Message =
                    "Customer cannot be deactivated because an active future booking exists."
            };
        }

        customer.Status = CustomerStatus.Deactivated;
        customer.UpdatedAt = DateTime.UtcNow;

        _customerRepository.Update(customer);

        await _customerRepository.SaveChangesAsync();

        return new CustomerStatusChangeResult
        {
            Success = true,
            Message = "Customer account deactivated successfully."
        };
    }

    public async Task<CustomerStatusChangeResult> ReactivateAsync(
    int customerId)
    {
        var customer =
            await _customerRepository.GetByIdAsync(customerId);

        if (customer is null)
        {
            return new CustomerStatusChangeResult
            {
                Success = false,
                ErrorCode = "CUSTOMER_NOT_FOUND",
                Message = "Customer not found."
            };
        }

        if (customer.Status == CustomerStatus.Active)
        {
            return new CustomerStatusChangeResult
            {
                Success = true,
                Message = "Customer account is already active."
            };
        }

        customer.Status = CustomerStatus.Active;
        customer.UpdatedAt = DateTime.UtcNow;

        _customerRepository.Update(customer);

        await _customerRepository.SaveChangesAsync();

        return new CustomerStatusChangeResult
        {
            Success = true,
            Message = "Customer account reactivated successfully."
        };
    }
}
