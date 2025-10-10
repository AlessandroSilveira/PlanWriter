using System;
using System.Collections.Generic;

namespace PlanWriter.Domain.Entities;

// PlanWriter.Domain/Entities/Region.cs
public class Region
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;    // ex.: "Brasil - SP"
    public string Slug { get; set; } = string.Empty;    // ex.: "brasil-sp"
    public string? CountryCode { get; set; }           // opcional
    public ICollection<User> Users { get; set; } = new List<User>();
}
