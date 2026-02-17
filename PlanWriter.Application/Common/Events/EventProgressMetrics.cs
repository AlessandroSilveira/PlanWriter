namespace PlanWriter.Application.Common.Events;

public sealed record EventProgressMetrics(
    int TargetWords,
    int TotalWords,
    int Percent,
    int RemainingWords,
    bool Won
);
