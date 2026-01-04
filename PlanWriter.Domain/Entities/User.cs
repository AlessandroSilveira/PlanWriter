using System;
using System.Collections.Generic;

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
        public string? Bio { get; set; } 
        public string? AvatarUrl { get; set; } 
        public bool IsProfilePublic { get; set; }
        public string? Slug { get; set; }
        public string? DisplayName { get; set; }
        public Guid? RegionId { get; set; }
        public Region? Region { get; set; }

        public bool IsAdmin { get; private set; }
        public bool MustChangePassword { get; private set; }

        public void MakeAdmin()
        {
            IsAdmin = true;
            MustChangePassword = true; // admin SEMPRE troca senha inicial
        }

        // 👤 garante usuário comum
        public void MakeRegularUser()
        {
            IsAdmin = false;
            MustChangePassword = false;
        }

        public void ChangePassword(string newHash)
        {
            PasswordHash = newHash;

            // ✅ só admin controla essa flag
            if (IsAdmin)
                MustChangePassword = false;
        }
        
        
        public ICollection<UserFollow> Following { get; set; } = new List<UserFollow>();
        public ICollection<UserFollow> Followers { get; set; } = new List<UserFollow>();

    }
}// Entidade de usuário