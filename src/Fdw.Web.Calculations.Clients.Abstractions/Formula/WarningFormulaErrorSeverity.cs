using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Web.Calculations.Clients.Formula;

/// <summary>Warning that may affect correctness.</summary>
[TypeOption(typeof(FormulaErrorSeverities), "Warning")]
[ExcludeFromCodeCoverage]
public sealed class WarningFormulaErrorSeverity : FormulaErrorSeverityBase
{
    /// <summary>Initializes a new instance of <see cref="WarningFormulaErrorSeverity"/>.</summary>
    public WarningFormulaErrorSeverity() : base(2, "Warning") { }
}
