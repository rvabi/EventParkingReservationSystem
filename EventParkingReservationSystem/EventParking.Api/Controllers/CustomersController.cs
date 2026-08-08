using Microsoft.AspNetCore.Mvc;

using EventParking.Business.DTOs.Customers;
using EventParking.Business.Interfaces;
using EventParking.Models.Entities;
using EventParking.Models.Enums;


namespace EventParking.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;

    public CustomersController(
        ICustomerService customerService)
    {
        _customerService = customerService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CustomerDto>>> GetAll()
    {
        var customers = await _customerService.GetAllAsync();

        var customerDtos = customers
            .Select(MapToDto)
            .ToList();

        return Ok(customerDtos);
    }

    [HttpGet("{customerId:int}")]
    public async Task<ActionResult<CustomerDto>> GetById(
        int customerId)
    {
        var customer =
            await _customerService.GetByIdAsync(customerId);

        if (customer is null)
        {
            return NotFound(new
            {
                message = "Customer not found."
            });
        }

        return Ok(MapToDto(customer));
    }

    [HttpPut("{customerId:int}/profile")]
    public async Task<IActionResult> UpdateProfile(
        int customerId,
        [FromBody] UpdateCustomerProfileRequest request)
    {
        var existingCustomer =
            await _customerService.GetByIdAsync(customerId);

        if (existingCustomer is null)
        {
            return NotFound(new
            {
                message = "Customer not found."
            });
        }

        var customer = new Customer
        {
            Id = customerId,
            FullName = request.FullName,
            Email = request.Email,
            Phone = request.Phone
        };

        var updated =
            await _customerService.UpdateProfileAsync(customer);

        if (!updated)
        {
            return BadRequest(new
            {
                message =
                    "Unable to update customer profile. Check the submitted data or email address."
            });
        }

        var updatedCustomer =
            await _customerService.GetByIdAsync(customerId);

        return Ok(new
        {
            message = "Customer profile updated successfully.",
            customer = updatedCustomer is null
                ? null
                : MapToDto(updatedCustomer)
        });
    }

    [HttpPatch("{customerId:int}/status")]
    public async Task<IActionResult> ChangeStatus(
        int customerId,
        [FromBody] ChangeCustomerStatusRequest request)
    {
        if (!Enum.TryParse<CustomerStatus>(
                request.Status,
                true,
                out var status))
        {
            return BadRequest(new
            {
                message =
                    "Invalid customer status. Use Active or Deactivated."
            });
        }

        var existingCustomer =
            await _customerService.GetByIdAsync(customerId);

        if (existingCustomer is null)
        {
            return NotFound(new
            {
                message = "Customer not found."
            });
        }

        var changed =
            await _customerService.ChangeStatusAsync(
                customerId,
                status);

        if (!changed)
        {
            return BadRequest(new
            {
                message = "Unable to change customer status."
            });
        }

        return Ok(new
        {
            message =
                $"Customer status changed to {status} successfully."
        });
    }

    private static CustomerDto MapToDto(Customer customer)
    {
        return new CustomerDto
        {
            Id = customer.Id,
            FullName = customer.FullName,
            Email = customer.Email,
            Phone = customer.Phone,
            Role = customer.Role.ToString(),
            Status = customer.Status.ToString(),
            EmailVerified = customer.EmailVerified,
            CreatedAt = customer.CreatedAt,
            UpdatedAt = customer.UpdatedAt
        };
    }
}
