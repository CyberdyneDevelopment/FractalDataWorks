using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Web.Calculations.Clients.Formula;

/// <summary>Error that prevents the formula from being valid.</summary>
[TypeOption(typeof(FormulaErrorSeverities), "Error")]
[ExcludeFromCodeCoverage]
public sealed class ErrorFormulaErrorSeverity : FormulaErrorSeverityBase
{
    /// <summary>Initializes a new instance of <see cref="ErrorFormulaErrorSeverity"/>.</summary>
    public ErrorFormulaErrorSeverity() : base(3, "Error") { }
}
