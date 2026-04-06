using Microsoft.EntityFrameworkCore;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Interfaces;
using PaymentService.Infrastructure.Persistence;

namespace PaymentService.Infrastructure.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly PaymentDbContext _db;
    public PaymentRepository(PaymentDbContext db) => _db = db;

    public Task<Payment?> GetByIdAsync(Guid id) => _db.Payments.FindAsync(id).AsTask();
    public Task<Payment?> GetByOrderIdAsync(Guid orderId) => _db.Payments.FirstOrDefaultAsync(p => p.OrderId == orderId);
    public Task<List<Payment>> GetAllAsync() => _db.Payments.OrderByDescending(p => p.CreatedAt).ToListAsync();
    public async Task AddAsync(Payment p) => await _db.Payments.AddAsync(p);
    public Task UpdateAsync(Payment p) { _db.Payments.Update(p); return Task.CompletedTask; }
    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}
