using System;

namespace PlanWriter.Application.Common.Events;

public sealed class EventProgressCalculator : IEventProgressCalculator
{
    private const int DefaultTargetWords = 50000;

    public EventProgressMetrics Calculate(int? targetWords, int? totalWrittenInEvent)
    {
        var resolvedTarget = ResolveTarget(targetWords);
        var normalizedTotal = Math.Max(0, totalWrittenInEvent.GetValueOrDefault());

        var percent = (int)Math.Round(
            normalizedTotal * 100m / resolvedTarget,
            MidpointRounding.AwayFromZero);

        var remaining = Math.Max(0, resolvedTarget - normalizedTotal);

        return new EventProgressMetrics(
            TargetWords: resolvedTarget,
            TotalWords: normalizedTotal,
            Percent: percent,
            RemainingWords: remaining,
            Won: normalizedTotal >= resolvedTarget
        );
    }

    public DateTime ResolveWindowEndExclusive(DateTime endsAtUtc)
        => endsAtUtc.Date.AddDays(1);

    private static int ResolveTarget(int? targetWords)
    {
        var target = targetWords.GetValueOrDefault();
        return target > 0 ? target : DefaultTargetWords;
    }
}
