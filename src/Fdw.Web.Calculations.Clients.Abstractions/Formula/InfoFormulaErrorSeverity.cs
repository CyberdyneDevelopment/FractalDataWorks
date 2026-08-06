using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Web.Calculations.Clients.Formula;

/// <summary>Informational message.</summary>
[TypeOption(typeof(FormulaErrorSeverities), "Info")]
[ExcludeFromCodeCoverage]
public sealed class InfoFormulaErrorSeverity : FormulaErrorSeverityBase
{
    /// <summary>Initializes a new instance of <see cref="InfoFormulaErrorSeverity"/>.</summary>
    public InfoFormulaErrorSeverity() : base(1, "Info") { }
}
