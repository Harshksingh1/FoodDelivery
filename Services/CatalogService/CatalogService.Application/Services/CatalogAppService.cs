using CatalogService.Application.DTOs;
using CatalogService.Domain.Entities;
using CatalogService.Domain.Enums;
using CatalogService.Domain.Interfaces;

namespace CatalogService.Application.Services;

public class CatalogAppService
{
    private readonly IRestaurantRepository _repo;

    public CatalogAppService(IRestaurantRepository repo) => _repo = repo;

    // ── Public Discovery ──────────────────────────────────────────────────────

    public async Task<List<RestaurantDto>> GetRestaurantsAsync(string? city, string? cuisine, bool? isOpen)
        => (await _repo.GetAllAsync(city, cuisine, isOpen)).Select(Map).ToList();

    public async Task<List<RestaurantDto>> SearchRestaurantsAsync(string query)
        => (await _repo.SearchAsync(query)).Select(Map).ToList();

    public async Task<List<MenuItemDto>> SearchMenuAsync(string query)
        => (await _repo.SearchMenuAsync(query)).Select(MapItem).ToList();

    public async Task<(RestaurantDto? Data, List<MenuItemDto> Menu)> GetRestaurantDetailAsync(Guid id)
    {
        var r = await _repo.GetByIdAsync(id);
        if (r == null) return (null, []);
        var menu = await _repo.GetMenuAsync(id);
        return (Map(r), menu.Select(MapItem).ToList());
    }

    public async Task<List<MenuItemDto>> GetMenuAsync(Guid restaurantId)
        => (await _repo.GetMenuAsync(restaurantId)).Select(MapItem).ToList();

    // ── Partner — manage their approved restaurants ───────────────────────────

    public Task<List<RestaurantDto>> GetMyRestaurantsAsync(Guid ownerId)
        => _repo.GetByOwnerAsync(ownerId).ContinueWith(t => t.Result.Select(Map).ToList());

    public async Task<(bool Success, string Message)> DeleteRestaurantAsync(Guid restaurantId, Guid requesterId, bool isAdmin)
    {
        var r = await _repo.GetByIdAsync(restaurantId);
        if (r == null) return (false, "Restaurant not found.");
        if (!isAdmin && r.OwnerId != requesterId) return (false, "Access denied.");
        await _repo.DeleteAsync(r);
        await _repo.SaveChangesAsync();
        return (true, "Restaurant deleted.");
    }

    public async Task<(bool Success, string Message)> UpdateRestaurantAsync(Guid restaurantId, Guid ownerId, UpdateRestaurantRequest req)
    {
        var r = await _repo.GetByIdAsync(restaurantId);
        if (r == null || r.OwnerId != ownerId) return (false, "Restaurant not found or access denied.");
        r.Name = req.Name; r.Address = req.Address; r.City = req.City;
        r.Pincode = req.Pincode; r.CuisineType = req.CuisineType;
        r.PrepTimeMinutes = req.PrepTimeMinutes; r.MinOrderAmount = req.MinOrderAmount;
        r.UpdatedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(r); await _repo.SaveChangesAsync();
        return (true, "Restaurant updated.");
    }

    public async Task<(bool Success, string Message)> SetStatusAsync(Guid restaurantId, Guid ownerId, bool isOpen)
    {
        var r = await _repo.GetByIdAsync(restaurantId);
        if (r == null || r.OwnerId != ownerId) return (false, "Restaurant not found or access denied.");
        r.IsOpen = isOpen; r.UpdatedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(r); await _repo.SaveChangesAsync();
        return (true, "Status updated.");
    }

    public async Task<(bool Success, string Message, string? ImageUrl)> UploadRestaurantImageAsync(
        Guid restaurantId, Guid ownerId, Stream imageStream, string fileName)
    {
        var r = await _repo.GetByIdAsync(restaurantId);
        if (r == null || r.OwnerId != ownerId) return (false, "Access denied.", null);
        var url = await SaveImageAsync(imageStream, $"restaurants/{restaurantId}", fileName);
        r.ImageUrl = url; r.UpdatedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(r); await _repo.SaveChangesAsync();
        return (true, "Image uploaded.", url);
    }

    // ── Menu Items ────────────────────────────────────────────────────────────

    public async Task<(bool Success, string Message)> AddMenuItemAsync(Guid restaurantId, Guid ownerId, UpsertMenuItemRequest req)
    {
        var r = await _repo.GetByIdAsync(restaurantId);
        if (r == null || r.OwnerId != ownerId) return (false, "Restaurant not found or access denied.");
        await _repo.AddMenuItemAsync(new MenuItem
        {
            RestaurantId = restaurantId, Name = req.Name, Description = req.Description,
            Category = req.Category, Price = req.Price, IsVeg = req.IsVeg, IsAvailable = req.IsAvailable
        });
        await _repo.SaveChangesAsync();
        return (true, "Menu item added.");
    }

    public async Task<(bool Success, string Message)> UpdateMenuItemAsync(Guid itemId, Guid ownerId, UpsertMenuItemRequest req)
    {
        var item = await _repo.GetMenuItemAsync(itemId);
        if (item == null) return (false, "Item not found.");
        var r = await _repo.GetByIdAsync(item.RestaurantId);
        if (r == null || r.OwnerId != ownerId) return (false, "Access denied.");
        item.Name = req.Name; item.Description = req.Description; item.Category = req.Category;
        item.Price = req.Price; item.IsVeg = req.IsVeg; item.IsAvailable = req.IsAvailable;
        item.UpdatedAt = DateTime.UtcNow;
        await _repo.UpdateMenuItemAsync(item); await _repo.SaveChangesAsync();
        return (true, "Menu item updated.");
    }

    public async Task<(bool Success, string Message)> DeleteMenuItemAsync(Guid itemId, Guid ownerId)
    {
        var item = await _repo.GetMenuItemAsync(itemId);
        if (item == null) return (false, "Item not found.");
        var r = await _repo.GetByIdAsync(item.RestaurantId);
        if (r == null || r.OwnerId != ownerId) return (false, "Access denied.");
        await _repo.DeleteMenuItemAsync(item); await _repo.SaveChangesAsync();
        return (true, "Deleted.");
    }

    public async Task<(bool Success, string Message, string? ImageUrl)> UploadMenuItemImageAsync(
        Guid itemId, Guid ownerId, Stream imageStream, string fileName)
    {
        var item = await _repo.GetMenuItemAsync(itemId);
        if (item == null) return (false, "Item not found.", null);
        var r = await _repo.GetByIdAsync(item.RestaurantId);
        if (r == null || r.OwnerId != ownerId) return (false, "Access denied.", null);
        var url = await SaveImageAsync(imageStream, $"menu/{itemId}", fileName);
        item.ImageUrl = url; item.UpdatedAt = DateTime.UtcNow;
        await _repo.UpdateMenuItemAsync(item); await _repo.SaveChangesAsync();
        return (true, "Image uploaded.", url);
    }

    // ── Restaurant Applications ───────────────────────────────────────────────

    public async Task<(bool Success, string Message)> ApplyForRestaurantAsync(
        Guid userId, string applicantName, string applicantEmail, RestaurantApplicationRequest req)
    {
        var existing = await _repo.GetRestaurantApplicationByUserIdAsync(userId);
        if (existing != null && existing.Status == ApplicationStatus.Pending)
            return (false, "You already have a pending application.");

        await _repo.AddRestaurantApplicationAsync(new RestaurantApplication
        {
            UserId = userId, ApplicantName = applicantName, ApplicantEmail = applicantEmail,
            RestaurantName = req.RestaurantName, Address = req.Address, City = req.City,
            Pincode = req.Pincode, CuisineType = req.CuisineType, Gst = req.Gst, Fssai = req.Fssai
        });
        await _repo.SaveChangesAsync();
        return (true, "Application submitted. Awaiting admin approval.");
    }

    public async Task<(bool Success, string Message, RestaurantApplication? Data)> GetMyRestaurantApplicationAsync(Guid userId)
    {
        var app = await _repo.GetRestaurantApplicationByUserIdAsync(userId);
        return app == null ? (false, "No application found.", null) : (true, "Retrieved.", app);
    }

    public Task<List<RestaurantApplication>> GetAllRestaurantApplicationsAsync(ApplicationStatus? status)
        => _repo.GetAllRestaurantApplicationsAsync(status);

    public async Task<(bool Success, string Message)> ReviewRestaurantApplicationAsync(
        Guid applicationId, ReviewApplicationRequest req)
    {
        var app = await _repo.GetRestaurantApplicationByIdAsync(applicationId);
        if (app == null) return (false, "Application not found.");

        app.Status = req.Status;
        app.RejectionReason = req.RejectionReason;
        app.ReviewedAt = DateTime.UtcNow;

        if (req.Status == ApplicationStatus.Approved)
        {
            var restaurant = new Restaurant
            {
                OwnerId = app.UserId, Name = app.RestaurantName, Address = app.Address,
                City = app.City, Pincode = app.Pincode, CuisineType = app.CuisineType,
                Gst = app.Gst, Fssai = app.Fssai, IsActive = true, IsOpen = true
            };
            await _repo.AddAsync(restaurant);
            app.RestaurantId = restaurant.Id;
        }

        await _repo.UpdateRestaurantApplicationAsync(app);
        await _repo.SaveChangesAsync();
        return (true, $"Application {req.Status}.");
    }

    // ── Delivery Agent Applications ───────────────────────────────────────────

    public async Task<(bool Success, string Message)> ApplyForDeliveryAgentAsync(
        Guid userId, string applicantName, string applicantEmail, DeliveryAgentApplicationRequest req)
    {
        if (await _repo.HasApprovedAgentApplicationAsync(userId))
            return (false, "You are already an approved delivery agent.");

        var existing = await _repo.GetAgentApplicationByUserIdAsync(userId);
        if (existing != null && existing.Status == ApplicationStatus.Pending)
            return (false, "You already have a pending application.");

        await _repo.AddAgentApplicationAsync(new DeliveryAgentApplication
        {
            UserId = userId, ApplicantName = applicantName, ApplicantEmail = applicantEmail,
            Location = req.Location, AadhaarNumber = req.AadhaarNumber,
            VehicleType = req.VehicleType, VehicleNumber = req.VehicleNumber, LicenseNumber = req.LicenseNumber
        });
        await _repo.SaveChangesAsync();
        return (true, "Application submitted. Awaiting admin approval.");
    }

    public async Task<(bool Success, string Message, DeliveryAgentApplication? Data)> GetMyAgentApplicationAsync(Guid userId)
    {
        var app = await _repo.GetAgentApplicationByUserIdAsync(userId);
        return app == null ? (false, "No application found.", null) : (true, "Retrieved.", app);
    }

    public Task<List<DeliveryAgentApplication>> GetAllAgentApplicationsAsync(ApplicationStatus? status)
        => _repo.GetAllAgentApplicationsAsync(status);

    public async Task<(bool Success, string Message)> ReviewAgentApplicationAsync(
        Guid applicationId, ReviewApplicationRequest req)
    {
        var app = await _repo.GetAgentApplicationByIdAsync(applicationId);
        if (app == null) return (false, "Application not found.");
        app.Status = req.Status;
        app.RejectionReason = req.RejectionReason;
        app.ReviewedAt = DateTime.UtcNow;
        await _repo.UpdateAgentApplicationAsync(app);
        await _repo.SaveChangesAsync();
        return (true, $"Application {req.Status}.");
    }

    public async Task<(bool Success, string Message)> RateRestaurantAsync(Guid restaurantId, int stars)
    {
        if (stars < 1 || stars > 5) return (false, "Rating must be between 1 and 5.");
        var r = await _repo.GetByIdAsync(restaurantId);
        if (r == null) return (false, "Restaurant not found.");
        await _repo.RateRestaurantAsync(restaurantId, stars);
        return (true, "Rating submitted.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static async Task<string> SaveImageAsync(Stream stream, string folder, string fileName)
    {
        var ext = Path.GetExtension(fileName);
        var savedName = $"{folder}{ext}";
        var savePath = Path.Combine("wwwroot", savedName);
        Directory.CreateDirectory(Path.GetDirectoryName(savePath)!);
        await using var fs = File.Create(savePath);
        await stream.CopyToAsync(fs);
        return $"/{savedName}";
    }

    private static RestaurantDto Map(Restaurant r) => new()
    {
        Id = r.Id, Name = r.Name, Address = r.Address, City = r.City,
        CuisineType = r.CuisineType, ImageUrl = r.ImageUrl, Rating = r.Rating,
        IsOpen = r.IsOpen, PrepTimeMinutes = r.PrepTimeMinutes, MinOrderAmount = r.MinOrderAmount
    };

    private static MenuItemDto MapItem(MenuItem m) => new()
    {
        Id = m.Id, RestaurantId = m.RestaurantId,
        RestaurantName = m.Restaurant?.Name ?? string.Empty,
        Name = m.Name, Description = m.Description, Category = m.Category,
        Price = m.Price, ImageUrl = m.ImageUrl, IsVeg = m.IsVeg, IsAvailable = m.IsAvailable, Rating = m.Rating
    };
}
