using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Conventions;

/// <summary>
/// Marks a member whose <c>??</c> or <c>??=</c> fallbacks Mike approved, with the reason he gave. FDW055 reports
/// every other fallback: a missing value fails loud (a MessageLogging message and a non-success result) instead of
/// being replaced with an assumed one.
/// </summary>
/// <remarks>
/// Why a reason is required: the no-fallbacks rule allows a fallback only with Mike's explicit approval, and the
/// reason is the record of that approval where the code is read. An empty reason is itself reported (FDW-3580).
/// </remarks>
[AttributeUsage(
    AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Property | AttributeTargets.Field,
    AllowMultiple = false,
    Inherited = false)]
[ExcludeFromCodeCoverage]
public sealed class ApprovedFallbackAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApprovedFallbackAttribute"/> class.
    /// </summary>
    /// <param name="reason">Why Mike approved the fallback.</param>
    public ApprovedFallbackAttribute(string reason)
    {
        Reason = reason;
    }

    /// <summary>
    /// Gets why Mike approved the fallback.
    /// </summary>
    public string Reason { get; }
}
