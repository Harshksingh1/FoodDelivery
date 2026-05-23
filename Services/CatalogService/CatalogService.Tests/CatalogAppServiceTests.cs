using CatalogService.Application.DTOs;
using CatalogService.Application.Services;
using CatalogService.Domain.Entities;
using CatalogService.Domain.Enums;
using CatalogService.Domain.Interfaces;
using Moq;
using NUnit.Framework;

namespace CatalogService.Tests;

[TestFixture]
public class CatalogAppServiceTests
{
    private Mock<IRestaurantRepository> _repo;
    private CatalogAppService _sut;

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<IRestaurantRepository>();
        _sut = new CatalogAppService(_repo.Object);
    }

    // ── Get Restaurants ───────────────────────────────────────────────────────

    [Test]
    public async Task GetRestaurants_ReturnsAll_WhenNoFilter()
    {
        var restaurants = new List<Restaurant>
        {
            new() { Id = Guid.NewGuid(), Name = "Mirchi", City = "Delhi", IsActive = true },
            new() { Id = Guid.NewGuid(), Name = "Spice Garden", City = "Mumbai", IsActive = true }
        };
        _repo.Setup(r => r.GetAllAsync(null, null, null)).ReturnsAsync(restaurants);

        var result = await _sut.GetRestaurantsAsync(null, null, null);

        Assert.That(result.Count, Is.EqualTo(2));
    }

    [Test]
    public async Task GetRestaurants_ReturnsEmpty_WhenNoneExist()
    {
        _repo.Setup(r => r.GetAllAsync(null, null, null)).ReturnsAsync([]);

        var result = await _sut.GetRestaurantsAsync(null, null, null);

        Assert.That(result, Is.Empty);
    }

    // ── Delete Restaurant ─────────────────────────────────────────────────────

    [Test]
    public async Task DeleteRestaurant_NotFound_ReturnsFalse()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Restaurant?)null);

        var (success, message) = await _sut.DeleteRestaurantAsync(Guid.NewGuid(), Guid.NewGuid(), false);

        Assert.That(success, Is.False);
        Assert.That(message, Does.Contain("not found"));
    }

    [Test]
    public async Task DeleteRestaurant_WrongOwner_ReturnsFalse()
    {
        var ownerId = Guid.NewGuid();
        var restaurant = new Restaurant { Id = Guid.NewGuid(), OwnerId = ownerId };
        _repo.Setup(r => r.GetByIdAsync(restaurant.Id)).ReturnsAsync(restaurant);

        var (success, message) = await _sut.DeleteRestaurantAsync(restaurant.Id, Guid.NewGuid(), false);

        Assert.That(success, Is.False);
        Assert.That(message, Does.Contain("Access denied"));
    }

    [Test]
    public async Task DeleteRestaurant_Admin_CanDeleteAnyRestaurant()
    {
        var restaurant = new Restaurant { Id = Guid.NewGuid(), OwnerId = Guid.NewGuid() };
        _repo.Setup(r => r.GetByIdAsync(restaurant.Id)).ReturnsAsync(restaurant);
        _repo.Setup(r => r.DeleteAsync(restaurant)).Returns(Task.CompletedTask);
        _repo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var (success, _) = await _sut.DeleteRestaurantAsync(restaurant.Id, Guid.Empty, isAdmin: true);

        Assert.That(success, Is.True);
    }

    // ── Add Menu Item ─────────────────────────────────────────────────────────

    [Test]
    public async Task AddMenuItem_WrongOwner_ReturnsFalse()
    {
        var restaurant = new Restaurant { Id = Guid.NewGuid(), OwnerId = Guid.NewGuid() };
        _repo.Setup(r => r.GetByIdAsync(restaurant.Id)).ReturnsAsync(restaurant);

        var (success, message) = await _sut.AddMenuItemAsync(restaurant.Id, Guid.NewGuid(),
            new UpsertMenuItemRequest { Name = "Pizza", Price = 299 });

        Assert.That(success, Is.False);
        Assert.That(message, Does.Contain("access denied"));
    }

    [Test]
    public async Task AddMenuItem_ValidOwner_ReturnsSuccess()
    {
        var ownerId = Guid.NewGuid();
        var restaurant = new Restaurant { Id = Guid.NewGuid(), OwnerId = ownerId };
        _repo.Setup(r => r.GetByIdAsync(restaurant.Id)).ReturnsAsync(restaurant);
        _repo.Setup(r => r.AddMenuItemAsync(It.IsAny<MenuItem>())).Returns(Task.CompletedTask);
        _repo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var (success, _) = await _sut.AddMenuItemAsync(restaurant.Id, ownerId,
            new UpsertMenuItemRequest { Name = "Pizza", Price = 299 });

        Assert.That(success, Is.True);
    }

    // ── Restaurant Application ────────────────────────────────────────────────

    [Test]
    public async Task ApplyForRestaurant_PendingExists_ReturnsFalse()
    {
        var userId = Guid.NewGuid();
        var existing = new RestaurantApplication { UserId = userId, Status = ApplicationStatus.Pending };
        _repo.Setup(r => r.GetRestaurantApplicationByUserIdAsync(userId)).ReturnsAsync(existing);

        var (success, message) = await _sut.ApplyForRestaurantAsync(userId, "Test", "t@t.com",
            new RestaurantApplicationRequest { RestaurantName = "R", Address = "A", City = "C", Pincode = "110001", CuisineType = "Indian", Gst = "G", Fssai = "F" });

        Assert.That(success, Is.False);
        Assert.That(message, Does.Contain("pending application"));
    }

    [Test]
    public async Task ApplyForRestaurant_NoExisting_ReturnsSuccess()
    {
        _repo.Setup(r => r.GetRestaurantApplicationByUserIdAsync(It.IsAny<Guid>())).ReturnsAsync((RestaurantApplication?)null);
        _repo.Setup(r => r.AddRestaurantApplicationAsync(It.IsAny<RestaurantApplication>())).Returns(Task.CompletedTask);
        _repo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var (success, _) = await _sut.ApplyForRestaurantAsync(Guid.NewGuid(), "Test", "t@t.com",
            new RestaurantApplicationRequest { RestaurantName = "R", Address = "A", City = "C", Pincode = "110001", CuisineType = "Indian", Gst = "G", Fssai = "F" });

        Assert.That(success, Is.True);
    }
}
