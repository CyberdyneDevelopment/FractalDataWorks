using System;
using Fdw.Collections;
using Fdw.Data.Abstractions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Base class for data mappers between type systems.
/// Provides default CLR bridge implementation (Source → CLR → Target).
/// </summary>
/// <typeparam name="TSource">Source type system converter.</typeparam>
/// <typeparam name="TTarget">Target type system converter.</typeparam>
public abstract class DataMapperBase<TSource, TTarget>(
    string id,
    string name,
    TSource sourceConverter,
    TTarget targetConverter)
    : TypeOptionBase<string, DataMapperBase<TSource, TTarget>>(id, name),
      IDataMapper<TSource, TTarget>
    where TSource : IDataTypeConverter
    where TTarget : IDataTypeConverter
{
    /// <summary>
    /// Gets the source type system converter.
    /// </summary>
    public TSource SourceConverter { get; } = sourceConverter;

    /// <summary>
    /// Gets the target type system converter.
    /// </summary>
    public TTarget TargetConverter { get; } = targetConverter;

    /// <summary>
    /// Gets a value indicating whether this mapper can perform the mapping.
    /// Default: true. Override to add validation logic.
    /// </summary>
    public virtual bool CanMap => true;

    /// <summary>
    /// Default CLR bridge implementation: Source → CLR → Target (two conversions).
    /// Step 1: Source type → CLR via SourceConverter.ToClr()
    /// Step 2: CLR → Target type via TargetConverter.ToDb()
    /// </summary>
    /// <param name="sourceValue">The source value to map.</param>
    /// <returns>The mapped value in target type system.</returns>
    public virtual object? MapViaClr(object? sourceValue)
    {
        DataMapperBaseLog.MappingViaClr(
            NullLogger<DataMapperBase<TSource, TTarget>>.Instance, SourceConverter.Name, TargetConverter.Name);

        // Step 1: Source → CLR
        var clrValue = SourceConverter.ToClr(sourceValue);

        // Step 2: CLR → Target
        var targetValue = TargetConverter.ToDb(clrValue);

        return targetValue;
    }

    /// <summary>
    /// Maps a value from source to target type system.
    /// Override for explicit optimized mapping.
    /// Default implementation should delegate to MapViaClr.
    /// </summary>
    /// <param name="sourceValue">The source value to map.</param>
    /// <returns>The mapped value in target type system.</returns>
    public abstract object? Map(object? sourceValue);
}
