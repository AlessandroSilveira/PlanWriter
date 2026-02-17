using System;

namespace PlanWriter.Application.Common.WinnerEligibility;

public sealed class WinnerEligibilityService : IWinnerEligibilityService
{
    public WinnerEligibilityResult EvaluateForGoodies(
        DateTime nowUtc,
        DateTime eventEndsAtUtc,
        int targetWords,
        int totalWordsInEvent,
        bool isValidated,
        bool won)
    {
        if (targetWords <= 0)
        {
            return new WinnerEligibilityResult(
                IsEligible: false,
                CanValidate: false,
                Status: "invalid_target",
                Message: "A meta do evento não está configurada corretamente."
            );
        }

        if (isValidated && won)
        {
            return new WinnerEligibilityResult(
                IsEligible: true,
                CanValidate: false,
                Status: "eligible",
                Message: "Parabéns! Você liberou os goodies de vencedor."
            );
        }

        if (totalWordsInEvent >= targetWords)
        {
            return new WinnerEligibilityResult(
                IsEligible: false,
                CanValidate: true,
                Status: "pending_validation",
                Message: "Meta atingida. Faça a validação final para liberar os goodies."
            );
        }

        if (nowUtc < eventEndsAtUtc)
        {
            return new WinnerEligibilityResult(
                IsEligible: false,
                CanValidate: false,
                Status: "in_progress",
                Message: "Continue escrevendo para atingir a meta do evento."
            );
        }

        return new WinnerEligibilityResult(
            IsEligible: false,
            CanValidate: false,
            Status: "not_eligible",
            Message: "Meta não atingida dentro do período do evento."
        );
    }

    public WinnerEligibilityResult EvaluateForCertificate(bool isValidated, bool won)
    {
        if (isValidated && won)
        {
            return new WinnerEligibilityResult(
                IsEligible: true,
                CanValidate: false,
                Status: "eligible",
                Message: "Certificado liberado."
            );
        }

        return new WinnerEligibilityResult(
            IsEligible: false,
            CanValidate: false,
            Status: "not_eligible",
            Message: "O certificado é liberado apenas para participantes vencedores com validação final."
        );
    }
}
