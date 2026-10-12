using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Components;

/// <summary>Warning message, does not fail validation but should be reviewed.</summary>
[TypeOption(typeof(ValidationSeverities), "Warning")]
[ExcludeFromCodeCoverage]
public sealed class WarningValidationSeverity : ValidationSeverityBase
{
    /// <summary>Initializes a new instance of <see cref="WarningValidationSeverity"/>.</summary>
    public WarningValidationSeverity() : base(1, "Warning") { }
}
