using PlanWriter.Domain.Entities;

namespace PlanWriter.Domain.Interfaces.Auth;

public interface IJwtTokenGenerator
{
    string Generate(User user, bool adminMfaVerified = false);
}
