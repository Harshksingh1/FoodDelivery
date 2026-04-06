using AuthService.Application.DTOs;
using AuthService.Application.Interfaces;
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using AuthService.Domain.Interfaces;

namespace AuthService.Application.Services;

public class RestaurantAppService : IRestaurantAppService
{
    private readonly IRestaurantRepository _repo;
    private readonly IUserRepository _userRepo;

    public RestaurantAppService(IRestaurantRepository repo, IUserRepository userRepo)
    {
        _repo = repo;
        _userRepo = userRepo;
    }

    public async Task<(bool Success, string Message)> ApplyAsync(Guid userId, RestaurantApplicationRequest request)
    {
        // Allow multiple applications (multiple restaurants), but not if one is already pending
        var existing = await _repo.GetApplicationByUserIdAsync(userId);
        if (existing != null && existing.Status == ApplicationStatus.Pending)
            return (false, "You already have a pending application. Wait for it to be reviewed before applying again.");

        var application = new RestaurantApplication
        {
            UserId = userId,
            RestaurantName = request.RestaurantName,
            Address = request.Address,
            City = request.City,
            Pincode = request.Pincode,
            CuisineType = request.CuisineType,
            Gst = request.Gst,
            Fssai = request.Fssai
        };

        await _repo.AddApplicationAsync(application);
        await _repo.SaveChangesAsync();
        return (true, "Application submitted successfully. Awaiting admin approval.");
    }

    public async Task<(bool Success, string Message, RestaurantApplication? Data)> GetMyApplicationAsync(Guid userId)
    {
        var app = await _repo.GetApplicationByUserIdAsync(userId);
        return app == null
            ? (false, "No application found.", null)
            : (true, "Application retrieved.", app);
    }

    public async Task<(bool Success, string Message)> ReviewApplicationAsync(Guid applicationId, ReviewApplicationRequest request)
    {
        var app = await _repo.GetApplicationByIdAsync(applicationId);
        if (app == null) return (false, "Application not found.");

        app.Status = request.Status;
        app.RejectionReason = request.RejectionReason;
        app.ReviewedAt = DateTime.UtcNow;

        if (request.Status == ApplicationStatus.Approved)
        {
            var restaurant = new Restaurant
            {
                OwnerId = app.UserId,
                RestaurantName = app.RestaurantName,
                Address = app.Address,
                City = app.City,
                Pincode = app.Pincode,
                CuisineType = app.CuisineType,
                Gst = app.Gst,
                Fssai = app.Fssai
            };
            await _repo.AddRestaurantAsync(restaurant);
            app.RestaurantId = restaurant.Id;

            // Promote user role to RestaurantPartner (idempotent — safe if already has role)
            var user = await _userRepo.GetByIdAsync(app.UserId);
            if (user != null)
            {
                var roles = await _userRepo.GetRolesAsync(user);
                await _userRepo.EnsureRoleExistsAsync("RestaurantPartner");
                if (!roles.Contains("RestaurantPartner"))
                    await _userRepo.AddToRoleAsync(user, "RestaurantPartner");
                user.Role = Domain.Enums.UserRole.RestaurantPartner;
                user.UpdatedAt = DateTime.UtcNow;
                await _userRepo.UpdateAsync(user);
            }
        }

        await _repo.UpdateApplicationAsync(app);
        await _repo.SaveChangesAsync();
        return (true, $"Application {request.Status}.");
    }

    public Task<List<RestaurantApplication>> GetAllApplicationsAsync(ApplicationStatus? status) =>
        _repo.GetAllApplicationsAsync(status);

    public Task<List<Restaurant>> GetMyRestaurantsAsync(Guid ownerId) =>
        _repo.GetRestaurantsByOwnerAsync(ownerId);

    public async Task<(bool Success, string Message)> UpdateRestaurantAsync(
        Guid restaurantId, Guid ownerId, RestaurantApplicationRequest request)
    {
        var restaurant = await _repo.GetRestaurantByIdAsync(restaurantId);
        if (restaurant == null || restaurant.OwnerId != ownerId)
            return (false, "Restaurant not found or access denied.");

        restaurant.RestaurantName = request.RestaurantName;
        restaurant.Address = request.Address;
        restaurant.City = request.City;
        restaurant.Pincode = request.Pincode;
        restaurant.CuisineType = request.CuisineType;
        restaurant.Gst = request.Gst;
        restaurant.Fssai = request.Fssai;
        restaurant.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateRestaurantAsync(restaurant);
        await _repo.SaveChangesAsync();
        return (true, "Restaurant updated.");
    }

    public async Task<(bool Success, string Message, string? ImageUrl)> UploadRestaurantImageAsync(
        Guid restaurantId, Guid ownerId, Stream imageStream, string fileName)
    {
        var restaurant = await _repo.GetRestaurantByIdAsync(restaurantId);
        if (restaurant == null || restaurant.OwnerId != ownerId)
            return (false, "Restaurant not found or access denied.", null);

        var ext = Path.GetExtension(fileName);
        var savedName = $"restaurants/{restaurantId}{ext}";
        var savePath = Path.Combine("wwwroot", savedName);

        Directory.CreateDirectory(Path.GetDirectoryName(savePath)!);
        await using var fs = File.Create(savePath);
        await imageStream.CopyToAsync(fs);

        restaurant.ImageUrl = $"/{savedName}";
        restaurant.UpdatedAt = DateTime.UtcNow;
        await _repo.UpdateRestaurantAsync(restaurant);
        await _repo.SaveChangesAsync();

        return (true, "Image uploaded.", restaurant.ImageUrl);
    }

    public async Task<(bool Success, string Message)> AddMenuItemAsync(
        Guid restaurantId, Guid ownerId, MenuItemRequest request)
    {
        var restaurant = await _repo.GetRestaurantByIdAsync(restaurantId);
        if (restaurant == null || restaurant.OwnerId != ownerId)
            return (false, "Restaurant not found or access denied.");

        var item = new MenuItem
        {
            RestaurantId = restaurantId,
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            IsVeg = request.IsVeg,
            IsAvailable = request.IsAvailable
        };

        await _repo.AddMenuItemAsync(item);
        await _repo.SaveChangesAsync();
        return (true, "Menu item added.");
    }

    public async Task<(bool Success, string Message)> UpdateMenuItemAsync(
        Guid itemId, Guid ownerId, MenuItemRequest request)
    {
        var item = await _repo.GetMenuItemByIdAsync(itemId);
        if (item == null) return (false, "Menu item not found.");

        var restaurant = await _repo.GetRestaurantByIdAsync(item.RestaurantId);
        if (restaurant == null || restaurant.OwnerId != ownerId)
            return (false, "Access denied.");

        item.Name = request.Name;
        item.Description = request.Description;
        item.Price = request.Price;
        item.IsVeg = request.IsVeg;
        item.IsAvailable = request.IsAvailable;
        item.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateMenuItemAsync(item);
        await _repo.SaveChangesAsync();
        return (true, "Menu item updated.");
    }

    public async Task<(bool Success, string Message)> DeleteMenuItemAsync(Guid itemId, Guid ownerId)
    {
        var item = await _repo.GetMenuItemByIdAsync(itemId);
        if (item == null) return (false, "Menu item not found.");

        var restaurant = await _repo.GetRestaurantByIdAsync(item.RestaurantId);
        if (restaurant == null || restaurant.OwnerId != ownerId)
            return (false, "Access denied.");

        await _repo.DeleteMenuItemAsync(item);
        await _repo.SaveChangesAsync();
        return (true, "Menu item deleted.");
    }

    public async Task<(bool Success, string Message, string? ImageUrl)> UploadMenuItemImageAsync(
        Guid itemId, Guid ownerId, Stream imageStream, string fileName)
    {
        var item = await _repo.GetMenuItemByIdAsync(itemId);
        if (item == null) return (false, "Menu item not found.", null);

        var restaurant = await _repo.GetRestaurantByIdAsync(item.RestaurantId);
        if (restaurant == null || restaurant.OwnerId != ownerId)
            return (false, "Access denied.", null);

        var ext = Path.GetExtension(fileName);
        var savedName = $"menu/{itemId}{ext}";
        var savePath = Path.Combine("wwwroot", savedName);

        Directory.CreateDirectory(Path.GetDirectoryName(savePath)!);
        await using var fs = File.Create(savePath);
        await imageStream.CopyToAsync(fs);

        item.ImageUrl = $"/{savedName}";
        item.UpdatedAt = DateTime.UtcNow;
        await _repo.UpdateMenuItemAsync(item);
        await _repo.SaveChangesAsync();

        return (true, "Image uploaded.", item.ImageUrl);
    }

    public Task<List<MenuItem>> GetMenuItemsAsync(Guid restaurantId) =>
        _repo.GetMenuItemsByRestaurantAsync(restaurantId);
}
