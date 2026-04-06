namespace OrderService.Domain.Enums;

public enum OrderStatus
{
    DraftCart,
    CheckoutStarted,
    PaymentPending,
    Paid,
    RestaurantAccepted,
    Preparing,
    ReadyForPickup,
    PickedUp,
    OutForDelivery,
    Delivered,
    PaymentFailed,
    CancelRequested,
    Cancelled,
    RestaurantRejected,
    RefundInitiated,
    Refunded
}
