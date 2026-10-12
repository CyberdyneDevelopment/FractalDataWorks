using System;
using Fdw.Data.Abstractions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Default mapper that uses CLR bridge (no explicit mapping).
/// Created automatically when no explicit mapper is registered.
/// Uses two-step conversion: Source → CLR → Target.
/// </summary>
/// <typeparam name="TSource">Source type system converter.</typeparam>
/// <typeparam name="TTarget">Target type system converter.</typeparam>
public sealed class DefaultDataMapper<TSource, TTarget>(
    TSource sourceConverter,
    TTarget targetConverter)
    : DataMapperBase<TSource, TTarget>(
        id: $"Default_{sourceConverter.Name}_to_{targetConverter.Name}",
        name: $"{sourceConverter.Name} → {targetConverter.Name}",
        sourceConverter: sourceConverter,
        targetConverter: targetConverter)
    where TSource : IDataTypeConverter
    where TTarget : IDataTypeConverter
{
    /// <summary>
    /// Uses default CLR bridge (delegates to MapViaClr).
    /// No optimization - two conversions occur.
    /// </summary>
    /// <param name="sourceValue">The source value to map.</param>
    /// <returns>The mapped value via CLR intermediary.</returns>
    public override object? Map(object? sourceValue)
    {
        DefaultDataMapperLog.UsingDefaultMapper(
            NullLogger<DefaultDataMapper<TSource, TTarget>>.Instance, SourceConverter.Name, TargetConverter.Name);
        return MapViaClr(sourceValue);
    }
}
