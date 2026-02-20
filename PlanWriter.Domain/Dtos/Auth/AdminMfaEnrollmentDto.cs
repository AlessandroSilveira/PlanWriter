namespace PlanWriter.Domain.Dtos.Auth;

public sealed class AdminMfaEnrollmentDto
{
    public string SecretKey { get; init; } = string.Empty;
    public string OtpAuthUri { get; init; } = string.Empty;
}
