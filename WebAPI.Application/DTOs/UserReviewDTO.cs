namespace WebAPI.Application.DTOs;

public record UserReviewDTO(string ReviewerId,string? ReviewerName, decimal Rating, string? Comment, DateTime CreatedAt);