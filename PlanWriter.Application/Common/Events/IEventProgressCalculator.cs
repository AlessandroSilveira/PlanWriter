using System;

namespace PlanWriter.Application.Common.Events;

public interface IEventProgressCalculator
{
    EventProgressMetrics Calculate(int? projectTargetWords, int? eventDefaultTargetWords, int? totalWrittenInEvent);
    DateTime ResolveWindowEndExclusive(DateTime endsAtUtc);
}
