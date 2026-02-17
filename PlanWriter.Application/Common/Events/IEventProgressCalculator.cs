using System;

namespace PlanWriter.Application.Common.Events;

public interface IEventProgressCalculator
{
    EventProgressMetrics Calculate(int? targetWords, int? totalWrittenInEvent);
    DateTime ResolveWindowEndExclusive(DateTime endsAtUtc);
}
