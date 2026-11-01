using System.Collections.Generic;

namespace Fdw.Services.Etl.Abstractions;

/// <summary>
/// The Validate part of a stage request: the checks and what a failing record causes.
/// </summary>
/// <remarks>
/// A separate interface, not a member of <see cref="ITransformOperationSpec"/>: that interface has other implementers
/// that do not state checks, and this assembly targets netstandard2.0, which has no default interface members.
/// </remarks>
public interface ITransformCheckingSpec
{
    /// <summary>Gets the checks a record must pass, in the order they are asked.</summary>
    IReadOnlyList<ITransformCheckSpec> Checks { get; }

    /// <summary>Gets the name of the <c>ValidateFailActions</c> option a failing record causes.</summary>
    string? OnFail { get; }
}
