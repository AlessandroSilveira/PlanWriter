using System;

namespace PlanWriter.Application.Common.WinnerEligibility;

public interface IWinnerEligibilityService
{
    WinnerEligibilityResult EvaluateForGoodies(
        DateTime nowUtc,
        DateTime eventEndsAtUtc,
        int targetWords,
        int totalWordsInEvent,
        bool isValidated,
        bool won);

    WinnerEligibilityResult EvaluateForCertificate(bool isValidated, bool won);
}
