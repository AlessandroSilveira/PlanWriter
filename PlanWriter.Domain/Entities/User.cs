using System;

namespace PlanWriter.Domain.Entities
{
    public class User
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string? Bio { get; set; }           // 280 chars
        public string? AvatarUrl { get; set; }     // 256 chars
        public bool IsProfilePublic { get; set; }  // se o perfil é público
        public string? Slug { get; set; }          // ex.: "ale-silveira"
        public string? DisplayName { get; set; }
    }
}// Entidade de usuário