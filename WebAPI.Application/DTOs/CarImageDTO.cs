namespace WebAPI.Application.DTOs;

public record CarImageDTO(
    string Id,
    string Url,
    bool IsPrimary,
    int SortOrder
);
