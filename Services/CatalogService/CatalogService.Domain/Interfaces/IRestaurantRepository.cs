using CatalogService.Domain.Entities;
using CatalogService.Domain.Enums;

namespace CatalogService.Domain.Interfaces;

public interface IRestaurantRepository
{
    // Public read
    Task<List<Restaurant>> GetAllAsync(string? city, string? cuisine, bool? isOpen);
    Task<List<Restaurant>> SearchAsync(string query);
    Task<List<MenuItem>> SearchMenuAsync(string query);
    Task<Restaurant?> GetByIdAsync(Guid id);
    Task<List<Restaurant>> GetByOwnerAsync(Guid ownerId);
    Task<List<MenuItem>> GetMenuAsync(Guid restaurantId);
    Task<MenuItem?> GetMenuItemAsync(Guid itemId);

    // Partner manage
    Task AddAsync(Restaurant restaurant);
    Task UpdateAsync(Restaurant restaurant);
    Task DeleteAsync(Restaurant restaurant);
    Task AddMenuItemAsync(MenuItem item);
    Task UpdateMenuItemAsync(MenuItem item);
    Task DeleteMenuItemAsync(MenuItem item);

    // Applications
    Task<RestaurantApplication?> GetRestaurantApplicationByIdAsync(Guid id);
    Task<RestaurantApplication?> GetRestaurantApplicationByUserIdAsync(Guid userId);
    Task<List<RestaurantApplication>> GetAllRestaurantApplicationsAsync(ApplicationStatus? status);
    Task AddRestaurantApplicationAsync(RestaurantApplication app);
    Task UpdateRestaurantApplicationAsync(RestaurantApplication app);

    Task<DeliveryAgentApplication?> GetAgentApplicationByIdAsync(Guid id);
    Task<DeliveryAgentApplication?> GetAgentApplicationByUserIdAsync(Guid userId);
    Task<bool> HasApprovedAgentApplicationAsync(Guid userId);
    Task<List<DeliveryAgentApplication>> GetAllAgentApplicationsAsync(ApplicationStatus? status);
    Task AddAgentApplicationAsync(DeliveryAgentApplication app);
    Task UpdateAgentApplicationAsync(DeliveryAgentApplication app);

    Task SaveChangesAsync();
}
