using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Conventions;

/// <summary>
/// Marks a constant whose number a protocol or file format fixes, not a choice: the longest column name TDS carries, the most rows an Excel worksheet
/// holds. FDW073 accepts a <c>Max*</c>, <c>Maximum*</c>, <c>Default*</c> or <c>Limit*</c> constant that carries this attribute.
/// </summary>
/// <remarks>
/// Why a reason is required: a limit is configuration (Mike, 2026-11-04), so a number written in code needs the record of why no host could choose another.
/// The reason names the protocol or format and what in it fixes the number. An empty reason is itself reported (FDW-7059).
/// </remarks>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
[ExcludeFromCodeCoverage]
public sealed class FormatLimitAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FormatLimitAttribute"/> class.
    /// </summary>
    /// <param name="reason">The protocol or format that fixes the number, and how.</param>
    public FormatLimitAttribute(string reason)
    {
        Reason = reason;
    }

    /// <summary>
    /// Gets the protocol or format that fixes the number, and how.
    /// </summary>
    public string Reason { get; }
}
