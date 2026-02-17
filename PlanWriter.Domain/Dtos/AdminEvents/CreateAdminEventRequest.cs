using System;

namespace PlanWriter.Domain.Dtos.AdminEvents;


public record CreateAdminEventRequest(string Name, string Type,
    DateTime StartDate, DateTime EndDate,
    int? DefaultTargetWords,
    DateTime? ValidationWindowStartsAtUtc = null,
    DateTime? ValidationWindowEndsAtUtc = null,
    string? AllowedValidationSources = null)
{
        
}
