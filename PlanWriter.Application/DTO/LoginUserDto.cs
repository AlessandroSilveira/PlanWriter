namespace PlanWriter.Application.DTO;

public class LoginUserDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? MfaCode { get; set; }
    public string? BackupCode { get; set; }
}
