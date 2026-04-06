using FoodDelivery.Shared.Events;
using MassTransit;
using OrderService.Domain.Enums;
using OrderService.Domain.Interfaces;

namespace OrderService.Infrastructure.Saga;

public class OrderStateMachine : MassTransitStateMachine<OrderSagaState>
{
    public State AwaitingPayment { get; private set; } = null!;
    public State AwaitingRestaurant { get; private set; } = null!;
    public State Preparing { get; private set; } = null!;
    public State AwaitingPickup { get; private set; } = null!;
    public State InDelivery { get; private set; } = null!;
    public State Completed { get; private set; } = null!;
    public State Failed { get; private set; } = null!;

    public Event<OrderPlacedEvent> OrderPlaced { get; private set; } = null!;
    public Event<PaymentProcessedEvent> PaymentProcessed { get; private set; } = null!;
    public Event<OrderAcceptedEvent> OrderAccepted { get; private set; } = null!;
    public Event<OrderRejectedEvent> OrderRejected { get; private set; } = null!;
    public Event<OrderReadyEvent> OrderReady { get; private set; } = null!;
    public Event<DeliveryAssignedEvent> DeliveryAssigned { get; private set; } = null!;
    public Event<OrderPickedUpEvent> OrderPickedUp { get; private set; } = null!;
    public Event<OrderDeliveredEvent> OrderDelivered { get; private set; } = null!;
    public Event<OrderCancelledEvent> OrderCancelled { get; private set; } = null!;

    public OrderStateMachine()
    {
        InstanceState(x => x.CurrentState);

        Event(() => OrderPlaced, x => x.CorrelateById(ctx => ctx.Message.OrderId));
        Event(() => PaymentProcessed, x => x.CorrelateById(ctx => ctx.Message.OrderId));
        Event(() => OrderAccepted, x => x.CorrelateById(ctx => ctx.Message.OrderId));
        Event(() => OrderRejected, x => x.CorrelateById(ctx => ctx.Message.OrderId));
        Event(() => OrderReady, x => x.CorrelateById(ctx => ctx.Message.OrderId));
        Event(() => DeliveryAssigned, x => x.CorrelateById(ctx => ctx.Message.OrderId));
        Event(() => OrderPickedUp, x => x.CorrelateById(ctx => ctx.Message.OrderId));
        Event(() => OrderDelivered, x => x.CorrelateById(ctx => ctx.Message.OrderId));
        Event(() => OrderCancelled, x => x.CorrelateById(ctx => ctx.Message.OrderId));

        Initially(
            When(OrderPlaced)
                .Then(ctx =>
                {
                    ctx.Saga.CustomerId = ctx.Message.CustomerId;
                    ctx.Saga.RestaurantId = ctx.Message.RestaurantId;
                    ctx.Saga.TotalAmount = ctx.Message.TotalAmount;
                    ctx.Saga.PaymentMethod = ctx.Message.PaymentMethod;
                    ctx.Saga.CreatedAt = ctx.Message.PlacedAt;
                })
                .TransitionTo(AwaitingPayment)
        );

        During(AwaitingPayment,
            When(PaymentProcessed, ctx => ctx.Message.Success)
                .Then(ctx => ctx.Saga.PaymentId = ctx.Message.PaymentId)
                .TransitionTo(AwaitingRestaurant),
            When(PaymentProcessed, ctx => !ctx.Message.Success)
                .TransitionTo(Failed),
            When(OrderCancelled)
                .TransitionTo(Failed)
        );

        During(AwaitingRestaurant,
            When(OrderAccepted)
                .TransitionTo(Preparing),
            When(OrderRejected)
                .TransitionTo(Failed),
            When(OrderCancelled)
                .TransitionTo(Failed)
        );

        During(Preparing,
            When(OrderReady)
                .TransitionTo(AwaitingPickup)
        );

        During(AwaitingPickup,
            When(DeliveryAssigned)
                .Then(ctx => ctx.Saga.AgentId = ctx.Message.AgentId),
            When(OrderPickedUp)
                .TransitionTo(InDelivery)
        );

        During(InDelivery,
            When(OrderDelivered)
                .TransitionTo(Completed)
                .Finalize()
        );

        SetCompletedWhenFinalized();
    }
}
