namespace PlanWriter.Domain.Interfaces.Services;

public interface ICertificateService
{
    byte[] GenerateWinnerCertificate(string userName, string projectTitle, string eventName, int finalWords);
}
