using AuthService.Domain.Entities;
using AuthService.Domain.Enums;

namespace AuthService.Domain.Interfaces;

public interface IRestaurantRepository
{
    Task<RestaurantApplication?> GetApplicationByIdAsync(Guid id);
    Task<RestaurantApplication?> GetApplicationByUserIdAsync(Guid userId);
    Task<List<RestaurantApplication>> GetAllApplicationsAsync(ApplicationStatus? status = null);
    Task AddApplicationAsync(RestaurantApplication application);
    Task UpdateApplicationAsync(RestaurantApplication application);

    Task<Restaurant?> GetRestaurantByIdAsync(Guid id);
    Task<List<Restaurant>> GetRestaurantsByOwnerAsync(Guid ownerId);
    Task AddRestaurantAsync(Restaurant restaurant);
    Task UpdateRestaurantAsync(Restaurant restaurant);

    Task<MenuItem?> GetMenuItemByIdAsync(Guid id);
    Task<List<MenuItem>> GetMenuItemsByRestaurantAsync(Guid restaurantId);
    Task AddMenuItemAsync(MenuItem item);
    Task UpdateMenuItemAsync(MenuItem item);
    Task DeleteMenuItemAsync(MenuItem item);

    Task SaveChangesAsync();
}
