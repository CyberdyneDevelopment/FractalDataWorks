using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Pages;

/// <summary>Error activity.</summary>
[TypeOption(typeof(ActivitySeverities), "Error")]
[ExcludeFromCodeCoverage]
public sealed class ErrorActivitySeverity : ActivitySeverityBase
{
    /// <summary>Initializes a new instance of <see cref="ErrorActivitySeverity"/>.</summary>
    public ErrorActivitySeverity() : base(4, "Error") { }
}
