using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Components;

/// <summary>Informational message, does not affect validation.</summary>
[TypeOption(typeof(ValidationSeverities), "Information")]
[ExcludeFromCodeCoverage]
public sealed class InformationValidationSeverity : ValidationSeverityBase
{
    /// <summary>Initializes a new instance of <see cref="InformationValidationSeverity"/>.</summary>
    public InformationValidationSeverity() : base(0, "Information") { }
}
