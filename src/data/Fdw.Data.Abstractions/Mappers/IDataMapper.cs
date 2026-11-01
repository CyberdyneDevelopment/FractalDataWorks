using System;
using Fdw.Collections;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Maps data values between two type systems (e.g., MsSql int ↔ JsonSchema integer).
/// Supports explicit mapping or default CLR bridge.
/// </summary>
/// <typeparam name="TSource">Source type system converter.</typeparam>
/// <typeparam name="TTarget">Target type system converter.</typeparam>
public interface IDataMapper<TSource, TTarget> : ITypeOption<string, DataMapperBase<TSource, TTarget>>
    where TSource : IDataTypeConverter
    where TTarget : IDataTypeConverter
{
    /// <summary>
    /// Gets the source type system converter.
    /// </summary>
    TSource SourceConverter { get; }

    /// <summary>
    /// Gets the target type system converter.
    /// </summary>
    TTarget TargetConverter { get; }

    /// <summary>
    /// Gets a value indicating whether this mapper can perform the mapping.
    /// </summary>
    bool CanMap { get; }

    /// <summary>
    /// Maps a value from source type system to target type system.
    /// May use explicit mapping or CLR bridge.
    /// </summary>
    /// <param name="sourceValue">The source value to map.</param>
    /// <returns>The mapped value in target type system.</returns>
    object? Map(object? sourceValue);

    /// <summary>
    /// Maps via CLR bridge (two-step: Source → CLR → Target).
    /// Default implementation available in all mappers.
    /// </summary>
    /// <param name="sourceValue">The source value to map.</param>
    /// <returns>The mapped value via CLR intermediary.</returns>
    object? MapViaClr(object? sourceValue);
}
