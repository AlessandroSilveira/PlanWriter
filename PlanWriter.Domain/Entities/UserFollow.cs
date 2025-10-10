
using System;

namespace PlanWriter.Domain.Entities;

public class UserFollow
{
    public Guid FollowerId { get; set; }   // quem segue
    public Guid FolloweeId { get; set; }   // quem é seguido
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    // Navigation (ajuste o tipo se o seu usuário for outro)
    public User Follower { get; set; } = default!;
    public User Followee { get; set; } = default!;
}
