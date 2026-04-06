using CatalogService.Domain.Entities;
using CatalogService.Domain.Enums;
using CatalogService.Domain.Interfaces;
using CatalogService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CatalogService.Infrastructure.Repositories;

public class RestaurantRepository : IRestaurantRepository
{
    private readonly CatalogDbContext _db;
    public RestaurantRepository(CatalogDbContext db) => _db = db;

    public Task<List<Restaurant>> GetAllAsync(string? city, string? cuisine, bool? isOpen)
    {
        var q = _db.Restaurants.Where(r => r.IsActive);
        if (!string.IsNullOrEmpty(city)) q = q.Where(r => r.City.ToLower().Contains(city.ToLower()));
        if (!string.IsNullOrEmpty(cuisine)) q = q.Where(r => r.CuisineType.ToLower().Contains(cuisine.ToLower()));
        if (isOpen.HasValue) q = q.Where(r => r.IsOpen == isOpen.Value);
        return q.ToListAsync();
    }

    public Task<List<Restaurant>> SearchAsync(string query) =>
        _db.Restaurants.Where(r => r.IsActive &&
            (r.Name.ToLower().Contains(query.ToLower()) ||
             r.CuisineType.ToLower().Contains(query.ToLower())))
        .ToListAsync();

    public Task<List<MenuItem>> SearchMenuAsync(string query) =>
        _db.MenuItems
            .Include(m => m.Restaurant)
            .Where(m => m.IsAvailable && m.Restaurant.IsActive &&
                (m.Name.ToLower().Contains(query.ToLower()) ||
                 m.Category.ToLower().Contains(query.ToLower()) ||
                 m.Description.ToLower().Contains(query.ToLower())))
            .ToListAsync();

    public Task<Restaurant?> GetByIdAsync(Guid id) =>
        _db.Restaurants.Include(r => r.MenuItems).FirstOrDefaultAsync(r => r.Id == id);

    public Task<List<Restaurant>> GetByOwnerAsync(Guid ownerId) =>
        _db.Restaurants.Where(r => r.OwnerId == ownerId).ToListAsync();

    public Task<List<MenuItem>> GetMenuAsync(Guid restaurantId) =>
        _db.MenuItems.Where(m => m.RestaurantId == restaurantId).ToListAsync();

    public Task<MenuItem?> GetMenuItemAsync(Guid itemId) =>
        _db.MenuItems.FirstOrDefaultAsync(m => m.Id == itemId);

    public async Task AddAsync(Restaurant r) => await _db.Restaurants.AddAsync(r);
    public Task UpdateAsync(Restaurant r) { _db.Restaurants.Update(r); return Task.CompletedTask; }
    public Task DeleteAsync(Restaurant r) { _db.Restaurants.Remove(r); return Task.CompletedTask; }
    public async Task AddMenuItemAsync(MenuItem item) => await _db.MenuItems.AddAsync(item);
    public Task UpdateMenuItemAsync(MenuItem item) { _db.MenuItems.Update(item); return Task.CompletedTask; }
    public Task DeleteMenuItemAsync(MenuItem item) { _db.MenuItems.Remove(item); return Task.CompletedTask; }

    // Restaurant Applications
    public Task<RestaurantApplication?> GetRestaurantApplicationByIdAsync(Guid id) =>
        _db.RestaurantApplications.FirstOrDefaultAsync(a => a.Id == id);

    public Task<RestaurantApplication?> GetRestaurantApplicationByUserIdAsync(Guid userId) =>
        _db.RestaurantApplications.OrderByDescending(a => a.AppliedAt)
            .FirstOrDefaultAsync(a => a.UserId == userId);

    public Task<List<RestaurantApplication>> GetAllRestaurantApplicationsAsync(ApplicationStatus? status) =>
        status == null
            ? _db.RestaurantApplications.OrderByDescending(a => a.AppliedAt).ToListAsync()
            : _db.RestaurantApplications.Where(a => a.Status == status).OrderByDescending(a => a.AppliedAt).ToListAsync();

    public async Task AddRestaurantApplicationAsync(RestaurantApplication app) =>
        await _db.RestaurantApplications.AddAsync(app);

    public Task UpdateRestaurantApplicationAsync(RestaurantApplication app)
    {
        _db.RestaurantApplications.Update(app);
        return Task.CompletedTask;
    }

    // Delivery Agent Applications
    public Task<DeliveryAgentApplication?> GetAgentApplicationByIdAsync(Guid id) =>
        _db.DeliveryAgentApplications.FirstOrDefaultAsync(a => a.Id == id);

    public Task<DeliveryAgentApplication?> GetAgentApplicationByUserIdAsync(Guid userId) =>
        _db.DeliveryAgentApplications.OrderByDescending(a => a.AppliedAt)
            .FirstOrDefaultAsync(a => a.UserId == userId);

    public Task<bool> HasApprovedAgentApplicationAsync(Guid userId) =>
        _db.DeliveryAgentApplications.AnyAsync(a => a.UserId == userId && a.Status == ApplicationStatus.Approved);

    public Task<List<DeliveryAgentApplication>> GetAllAgentApplicationsAsync(ApplicationStatus? status) =>
        status == null
            ? _db.DeliveryAgentApplications.OrderByDescending(a => a.AppliedAt).ToListAsync()
            : _db.DeliveryAgentApplications.Where(a => a.Status == status).OrderByDescending(a => a.AppliedAt).ToListAsync();

    public async Task AddAgentApplicationAsync(DeliveryAgentApplication app) =>
        await _db.DeliveryAgentApplications.AddAsync(app);

    public Task UpdateAgentApplicationAsync(DeliveryAgentApplication app)
    {
        _db.DeliveryAgentApplications.Update(app);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}
