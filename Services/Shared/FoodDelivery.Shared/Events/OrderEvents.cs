namespace FoodDelivery.Shared.Events;

public record OrderPlacedEvent(
    Guid OrderId, Guid CustomerId, Guid RestaurantId,
    decimal TotalAmount, string PaymentMethod, DateTime PlacedAt);

public record PaymentProcessedEvent(
    Guid OrderId, Guid PaymentId, bool Success,
    string? FailureReason, DateTime ProcessedAt);

public record OrderAcceptedEvent(
    Guid OrderId, Guid RestaurantId, int PrepTimeMinutes, DateTime AcceptedAt);

public record OrderRejectedEvent(
    Guid OrderId, Guid RestaurantId, string Reason, DateTime RejectedAt);

public record OrderReadyEvent(
    Guid OrderId, Guid RestaurantId, DateTime ReadyAt);

public record DeliveryAssignedEvent(
    Guid OrderId, Guid AgentId, DateTime AssignedAt);

public record OrderPickedUpEvent(
    Guid OrderId, Guid AgentId, DateTime PickedUpAt);

public record OrderDeliveredEvent(
    Guid OrderId, Guid AgentId, DateTime DeliveredAt);

public record OrderCancelledEvent(
    Guid OrderId, string Reason, string CancelledBy, DateTime CancelledAt);

public record RefundInitiatedEvent(
    Guid OrderId, Guid PaymentId, decimal Amount, string Reason, DateTime InitiatedAt);
