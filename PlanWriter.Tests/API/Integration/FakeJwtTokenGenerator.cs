using PlanWriter.Domain.Entities;
using PlanWriter.Domain.Interfaces.Auth;

namespace PlanWriter.Tests.API.Integration;

public sealed class FakeJwtTokenGenerator : IJwtTokenGenerator
{
    public string Generate(User user, bool adminMfaVerified = false)
    {
        return $"fake-jwt-{user.Id}-{adminMfaVerified}";
    }
}
