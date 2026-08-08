using EventParking.DataAccess.Context;
using EventParking.DataAccess.Interfaces;
using EventParking.Models.Entities;
using EventParking.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace EventParking.DataAccess.Repositories;

public class FoodOrderRepository : IFoodOrderRepository
{
    private readonly ApplicationDbContext _context;

    public FoodOrderRepository(
        ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<FoodOrder?> GetByIdAsync(
        int foodOrderId)
    {
        return await _context.FoodOrders
            .Include(foodOrder => foodOrder.Booking)
            .Include(foodOrder => foodOrder.Customer)
            .Include(foodOrder => foodOrder.FoodStall)
            .Include(foodOrder => foodOrder.Items)
                .ThenInclude(orderItem => orderItem.FoodItem)
            .FirstOrDefaultAsync(
                foodOrder => foodOrder.Id == foodOrderId);
    }

    public async Task<IReadOnlyList<FoodOrder>>
        GetByCustomerIdAsync(int customerId)
    {
        return await _context.FoodOrders
            .AsNoTracking()
            .Include(foodOrder => foodOrder.FoodStall)
            .Include(foodOrder => foodOrder.Items)
                .ThenInclude(orderItem => orderItem.FoodItem)
            .Where(foodOrder =>
                foodOrder.CustomerId == customerId)
            .OrderByDescending(foodOrder =>
                foodOrder.CreatedAt)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<FoodOrder>>
        GetAllAsync(
            FoodOrderStatus? status = null)
    {
        var query = _context.FoodOrders
            .AsNoTracking()
            .Include(foodOrder => foodOrder.Booking)
            .Include(foodOrder => foodOrder.Customer)
            .Include(foodOrder => foodOrder.FoodStall)
            .Include(foodOrder => foodOrder.Items)
                .ThenInclude(orderItem => orderItem.FoodItem)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(foodOrder =>
                foodOrder.Status == status.Value);
        }

        return await query
            .OrderByDescending(foodOrder =>
                foodOrder.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> OrderNumberExistsAsync(
        string orderNumber)
    {
        return await _context.FoodOrders
            .AnyAsync(foodOrder =>
                foodOrder.OrderNumber == orderNumber);
    }

    public async Task AddAsync(
        FoodOrder foodOrder)
    {
        await _context.FoodOrders.AddAsync(foodOrder);
    }

    public void Update(
        FoodOrder foodOrder)
    {
        _context.FoodOrders.Update(foodOrder);
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}