using EventParking.Models.Entities;
using EventParking.Models.Enums;

namespace EventParking.DataAccess.Interfaces;

public interface IFoodOrderRepository
{
    Task<FoodOrder?> GetByIdAsync(int foodOrderId);

    Task<IReadOnlyList<FoodOrder>> GetByCustomerIdAsync(
        int customerId);

    Task<IReadOnlyList<FoodOrder>> GetAllAsync(
        FoodOrderStatus? status = null);

    Task<bool> OrderNumberExistsAsync(
        string orderNumber);

    Task AddAsync(FoodOrder foodOrder);

    void Update(FoodOrder foodOrder);

    Task<int> SaveChangesAsync();
}
