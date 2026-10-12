using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Pages;

/// <summary>Warning activity.</summary>
[TypeOption(typeof(ActivitySeverities), "Warning")]
[ExcludeFromCodeCoverage]
public sealed class WarningActivitySeverity : ActivitySeverityBase
{
    /// <summary>Initializes a new instance of <see cref="WarningActivitySeverity"/>.</summary>
    public WarningActivitySeverity() : base(3, "Warning") { }
}
