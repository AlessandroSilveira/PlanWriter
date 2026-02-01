// using System;
// using PlanWriter.Domain.Interfaces.Services;
// using QuestPDF.Fluent;
// using QuestPDF.Helpers;
// using QuestPDF.Infrastructure;
//
// namespace PlanWriter.Application.Services;
//
// public class CertificateService : ICertificateService
// {
//     public byte[] GenerateWinnerCertificate(string userName, string projectTitle, string eventName, int finalWords)
//     {
//         QuestPDF.Settings.License = LicenseType.Community;
//
//         return QuestPDF.Fluent.Document.Create(container =>
//         {
//             container.Page(page =>
//             {
//                 page.Margin(40);
//                 page.Background(Colors.Grey.Lighten3);
//                 page.Header().AlignCenter().Text(eventName).FontSize(24).SemiBold();
//                 page.Content().AlignCenter().Column(col =>
//                 {
//                     col.Item().Text("CERTIFICADO DE VENCEDOR").FontSize(32).Bold();
//                     col.Item().PaddingTop(20).Text(userName).FontSize(22);
//                     col.Item().Text($"venceu com o projeto “{projectTitle}”").FontSize(16);
//                     col.Item().PaddingTop(10).Text($"Palavras no evento: {finalWords:N0}").FontSize(14);
//                     col.Item().PaddingTop(20).Text("Parabéns!").FontSize(18);
//                 });
//                 page.Footer().AlignRight().Text(DateTime.UtcNow.ToString("dd/MM/yyyy")).FontSize(10);
//             });
//         }).GeneratePdf();
//     }
// }