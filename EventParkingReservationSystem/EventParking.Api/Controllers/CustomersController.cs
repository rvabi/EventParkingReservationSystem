using Microsoft.AspNetCore.Mvc;

using EventParking.Business.DTOs.Customers;
using EventParking.Business.Interfaces;
using EventParking.Models.Entities;
using EventParking.Models.Enums;

using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;


namespace EventParking.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;

    public CustomersController(
        ICustomerService customerService)
    {
        _customerService = customerService;
    }

    
    [HttpGet]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<IEnumerable<CustomerDto>>> GetAll()
    {
        var customers = await _customerService.GetAllAsync();

        var customerDtos = customers
            .Select(MapToDto)
            .ToList();

        return Ok(customerDtos);
    }

    [HttpGet("search")]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<IEnumerable<CustomerDto>>> Search(
    [FromQuery] string searchTerm)
    {
        var customers =
            await _customerService.SearchAsync(searchTerm);

        var customerDtos =
            customers
                .Select(MapToDto)
                .ToList();

        return Ok(customerDtos);
    }

    [HttpGet("{customerId:int}")]
    [Authorize(Roles = "Administrator")]
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

    [HttpGet("me")]
    [Authorize(Roles = "Customer")]
    public async Task<ActionResult<CustomerDto>> GetMyProfile()
    {
        int? customerId = GetAuthenticatedCustomerId();

        if (!customerId.HasValue)
        {
            return Unauthorized(new
            {
                message = "Invalid authentication token."
            });
        }

        var customer =
            await _customerService.GetByIdAsync(
                customerId.Value);

        if (customer is null)
        {
            return NotFound(new
            {
                message = "Customer profile not found."
            });
        }

        return Ok(MapToDto(customer));
    }

    [HttpPut("me")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> UpdateMyProfile(
    [FromBody] UpdateCustomerProfileRequest request)
    {
        int? customerId = GetAuthenticatedCustomerId();

        if (!customerId.HasValue)
        {
            return Unauthorized(new
            {
                message = "Invalid authentication token."
            });
        }

        var existingCustomer =
            await _customerService.GetByIdAsync(
                customerId.Value);

        if (existingCustomer is null)
        {
            return NotFound(new
            {
                message = "Customer profile not found."
            });
        }

        var customer = new Customer
        {
            Id = customerId.Value,
            FullName = request.FullName,
            Email = request.Email,
            Phone = request.Phone
        };

        bool updated =
            await _customerService.UpdateProfileAsync(customer);

        if (!updated)
        {
            return BadRequest(new
            {
                message =
                    "Unable to update profile. Check the submitted data or email address."
            });
        }

        var updatedCustomer =
            await _customerService.GetByIdAsync(
                customerId.Value);

        return Ok(new
        {
            message = "Customer profile updated successfully.",
            customer = updatedCustomer is null
                ? null
                : MapToDto(updatedCustomer)
        });
    }

    [HttpPatch("{customerId:int}/deactivate")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Deactivate(
    int customerId)
    {
        var result =
            await _customerService.DeactivateAsync(
                customerId);

        if (!result.Success)
        {
            if (result.ErrorCode == "CUSTOMER_NOT_FOUND")
            {
                return NotFound(new
                {
                    message = result.Message
                });
            }

            if (result.ErrorCode == "ACTIVE_FUTURE_BOOKING")
            {
                return Conflict(new
                {
                    message = result.Message
                });
            }

            return BadRequest(new
            {
                message = result.Message
            });
        }

        return Ok(new
        {
            message = result.Message
        });
    }

    [HttpPatch("{customerId:int}/reactivate")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Reactivate(
    int customerId)
    {
        var result =
            await _customerService.ReactivateAsync(
                customerId);

        if (!result.Success)
        {
            if (result.ErrorCode == "CUSTOMER_NOT_FOUND")
            {
                return NotFound(new
                {
                    message = result.Message
                });
            }

            return BadRequest(new
            {
                message = result.Message
            });
        }

        return Ok(new
        {
            message = result.Message
        });
    }

    private int? GetAuthenticatedCustomerId()
    {
        string? customerIdValue =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(customerIdValue, out int customerId))
        {
            return null;
        }

        return customerId;
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
