namespace PlanWriter.API.Security;

public sealed class AuthBootstrapOptions
{
    public bool Enabled { get; set; }
    public string AdminEmail { get; set; } = string.Empty;
    public string AdminPassword { get; set; } = string.Empty;
    public string AdminFirstName { get; set; } = "Admin";
    public string AdminLastName { get; set; } = "System";
}
