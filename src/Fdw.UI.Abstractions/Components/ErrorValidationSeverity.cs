using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Components;

/// <summary>Error message, causes validation to fail.</summary>
[TypeOption(typeof(ValidationSeverities), "Error")]
[ExcludeFromCodeCoverage]
public sealed class ErrorValidationSeverity : ValidationSeverityBase
{
    /// <summary>Initializes a new instance of <see cref="ErrorValidationSeverity"/>.</summary>
    public ErrorValidationSeverity() : base(2, "Error") { }
}
