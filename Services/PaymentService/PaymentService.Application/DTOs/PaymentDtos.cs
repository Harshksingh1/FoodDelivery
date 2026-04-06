using System.ComponentModel.DataAnnotations;

namespace PaymentService.Application.DTOs;

public class SimulatePaymentRequest
{
    [Required] public Guid OrderId { get; set; }
    [Required] public Guid CustomerId { get; set; }
    [Required] public decimal Amount { get; set; }
    [Required] public string Method { get; set; } = "COD"; // COD, Card, Wallet
    public bool SimulateFailure { get; set; } = false;
}

public class RefundRequest
{
    [Required] public Guid PaymentId { get; set; }
    [Required] public string Reason { get; set; } = string.Empty;
}
