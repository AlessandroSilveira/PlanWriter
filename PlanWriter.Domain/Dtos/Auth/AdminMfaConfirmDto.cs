namespace PlanWriter.Domain.Dtos.Auth;

public sealed class AdminMfaConfirmDto
{
    public string Code { get; set; } = string.Empty;
}
