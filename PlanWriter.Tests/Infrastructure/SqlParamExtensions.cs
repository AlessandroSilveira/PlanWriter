using System;

namespace PlanWriter.Tests.Infrastructure;

internal static class SqlParamExtensions
{
    public static T GetProp<T>(this object param, string propertyName)
    {
        var prop = param.GetType().GetProperty(propertyName);
        if (prop is null)
            throw new InvalidOperationException($"Property '{propertyName}' not found on SQL param object.");

        return (T)prop.GetValue(param)!;
    }
}
