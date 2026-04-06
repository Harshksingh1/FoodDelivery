using MassTransit;

namespace OrderService.Infrastructure.Saga;

public class OrderSagaState : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; } // = OrderId
    public string CurrentState { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public Guid RestaurantId { get; set; }
    public decimal TotalAmount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public Guid? PaymentId { get; set; }
    public Guid? AgentId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
