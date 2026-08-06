using Fdw.Collections;

namespace Fdw.Web.Calculations.Clients.Formula;

/// <summary>
/// Base class for formula error severity levels.
/// </summary>
public abstract class FormulaErrorSeverityBase : TypeOptionBase<int, FormulaErrorSeverityBase>, IFormulaErrorSeverity
{
    /// <summary>
    /// Initializes a new instance of <see cref="FormulaErrorSeverityBase"/>.
    /// </summary>
    protected FormulaErrorSeverityBase(int id, string name) : base(id, name) { }
}
