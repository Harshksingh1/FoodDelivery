using System.ComponentModel.DataAnnotations;
using AuthService.Domain.Enums;

namespace AuthService.Application.DTOs;

public class ReviewApplicationRequest
{
    [Required] public ApplicationStatus Status { get; set; }
    public string? RejectionReason { get; set; }
}
