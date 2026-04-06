using AuthService.Application.DTOs;
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;

namespace AuthService.Application.Interfaces;

public interface IRestaurantAppService
{
    // Partner — apply for restaurant onboarding
    Task<(bool Success, string Message)> ApplyAsync(Guid userId, RestaurantApplicationRequest request);
    Task<(bool Success, string Message, RestaurantApplication? Data)> GetMyApplicationAsync(Guid userId);

    // Admin — review applications
    Task<(bool Success, string Message)> ReviewApplicationAsync(Guid applicationId, ReviewApplicationRequest request);
    Task<List<RestaurantApplication>> GetAllApplicationsAsync(ApplicationStatus? status);

    // Partner — manage their restaurants
    Task<List<Restaurant>> GetMyRestaurantsAsync(Guid ownerId);
    Task<(bool Success, string Message)> UpdateRestaurantAsync(Guid restaurantId, Guid ownerId, RestaurantApplicationRequest request);
    Task<(bool Success, string Message, string? ImageUrl)> UploadRestaurantImageAsync(Guid restaurantId, Guid ownerId, Stream imageStream, string fileName);

    // Menu items
    Task<(bool Success, string Message)> AddMenuItemAsync(Guid restaurantId, Guid ownerId, MenuItemRequest request);
    Task<(bool Success, string Message)> UpdateMenuItemAsync(Guid itemId, Guid ownerId, MenuItemRequest request);
    Task<(bool Success, string Message)> DeleteMenuItemAsync(Guid itemId, Guid ownerId);
    Task<(bool Success, string Message, string? ImageUrl)> UploadMenuItemImageAsync(Guid itemId, Guid ownerId, Stream imageStream, string fileName);
    Task<List<MenuItem>> GetMenuItemsAsync(Guid restaurantId);
}
