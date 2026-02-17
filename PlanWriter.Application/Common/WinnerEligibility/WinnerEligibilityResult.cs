namespace PlanWriter.Application.Common.WinnerEligibility;

public sealed record WinnerEligibilityResult(
    bool IsEligible,
    bool CanValidate,
    string Status,
    string Message
);
