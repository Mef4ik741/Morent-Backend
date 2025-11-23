namespace WebAPI.Application.DTOs;

public class OwnerDecisionRequest
{
    public bool IsApproved { get; set; }
    public string? Message { get; set; }
}
