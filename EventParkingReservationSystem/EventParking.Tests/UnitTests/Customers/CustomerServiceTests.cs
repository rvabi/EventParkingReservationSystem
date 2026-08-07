using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EventParking.Business.Services;
using EventParking.DataAccess.Interfaces;
using EventParking.Models.Entities;
using EventParking.Models.Enums;
using Moq;

namespace EventParking.Tests.UnitTests.Customers;

public class CustomerServiceTests
{
    private readonly Mock<ICustomerRepository> _repositoryMock;
    private readonly CustomerService _customerService;

    public CustomerServiceTests()
    {
        _repositoryMock = new Mock<ICustomerRepository>();

        _customerService =
            new CustomerService(_repositoryMock.Object);
    }

    [Fact]
    public async Task GetByIdAsync_ValidId_ReturnsCustomer()
    {
        // Arrange
        var customer = new Customer
        {
            Id = 1,
            FullName = "Demo Customer",
            Email = "customer@eventparking.local",
            Phone = "0710000000",
            Role = UserRole.Customer,
            Status = CustomerStatus.Active,
            EmailVerified = true
        };

        _repositoryMock
            .Setup(repository => repository.GetByIdAsync(1))
            .ReturnsAsync(customer);

        // Act
        var result =
            await _customerService.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);

        Assert.Equal(1, result.Id);
        Assert.Equal("Demo Customer", result.FullName);
        Assert.Equal(
            "customer@eventparking.local",
            result.Email);

        _repositoryMock.Verify(
            repository => repository.GetByIdAsync(1),
            Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_InvalidId_ReturnsNull()
    {
        // Act
        var result =
            await _customerService.GetByIdAsync(0);

        // Assert
        Assert.Null(result);

        _repositoryMock.Verify(
            repository => repository.GetByIdAsync(
                It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateProfileAsync_ValidData_UpdatesCustomer()
    {
        // Arrange
        var existingCustomer = new Customer
        {
            Id = 2,
            FullName = "Old Name",
            Email = "old@example.com",
            Phone = "0710000000",
            Role = UserRole.Customer,
            Status = CustomerStatus.Active,
            EmailVerified = true
        };

        var requestedCustomer = new Customer
        {
            Id = 2,
            FullName = "Updated Customer",
            Email = "new@example.com",
            Phone = "0771234567"
        };

        _repositoryMock
            .Setup(repository =>
                repository.GetByIdAsync(2))
            .ReturnsAsync(existingCustomer);

        _repositoryMock
            .Setup(repository =>
                repository.EmailExistsAsync(
                    "new@example.com"))
            .ReturnsAsync(false);

        _repositoryMock
            .Setup(repository =>
                repository.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result =
            await _customerService.UpdateProfileAsync(
                requestedCustomer);

        // Assert
        Assert.True(result);

        Assert.Equal(
            "Updated Customer",
            existingCustomer.FullName);

        Assert.Equal(
            "new@example.com",
            existingCustomer.Email);

        Assert.Equal(
            "0771234567",
            existingCustomer.Phone);

        Assert.NotNull(existingCustomer.UpdatedAt);

        _repositoryMock.Verify(
            repository =>
                repository.Update(existingCustomer),
            Times.Once);

        _repositoryMock.Verify(
            repository =>
                repository.SaveChangesAsync(),
            Times.Once);
    }

    [Fact]
    public async Task UpdateProfileAsync_DuplicateEmail_ReturnsFalse()
    {
        // Arrange
        var existingCustomer = new Customer
        {
            Id = 2,
            FullName = "Demo Customer",
            Email = "customer@example.com",
            Phone = "0710000000",
            Role = UserRole.Customer,
            Status = CustomerStatus.Active
        };

        var requestedCustomer = new Customer
        {
            Id = 2,
            FullName = "Demo Customer Updated",
            Email = "admin@example.com",
            Phone = "0771234567"
        };

        _repositoryMock
            .Setup(repository =>
                repository.GetByIdAsync(2))
            .ReturnsAsync(existingCustomer);

        _repositoryMock
            .Setup(repository =>
                repository.EmailExistsAsync(
                    "admin@example.com"))
            .ReturnsAsync(true);

        // Act
        var result =
            await _customerService.UpdateProfileAsync(
                requestedCustomer);

        // Assert
        Assert.False(result);

        _repositoryMock.Verify(
            repository =>
                repository.Update(
                    It.IsAny<Customer>()),
            Times.Never);

        _repositoryMock.Verify(
            repository =>
                repository.SaveChangesAsync(),
            Times.Never);
    }

    [Fact]
    public async Task ChangeStatusAsync_ValidCustomer_ChangesStatus()
    {
        // Arrange
        var customer = new Customer
        {
            Id = 2,
            FullName = "Demo Customer",
            Email = "customer@example.com",
            Phone = "0710000000",
            Role = UserRole.Customer,
            Status = CustomerStatus.Active
        };

        _repositoryMock
            .Setup(repository =>
                repository.GetByIdAsync(2))
            .ReturnsAsync(customer);

        _repositoryMock
            .Setup(repository =>
                repository.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result =
            await _customerService.ChangeStatusAsync(
                2,
                CustomerStatus.Deactivated);

        // Assert
        Assert.True(result);

        Assert.Equal(
            CustomerStatus.Deactivated,
            customer.Status);

        Assert.NotNull(customer.UpdatedAt);

        _repositoryMock.Verify(
            repository => repository.Update(customer),
            Times.Once);

        _repositoryMock.Verify(
            repository =>
                repository.SaveChangesAsync(),
            Times.Once);
    }
}
