using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Web.Calculations.Clients.Formula;

/// <summary>
/// TypeCollection for formula error severity levels.
/// </summary>
[TypeCollection(typeof(FormulaErrorSeverityBase), typeof(IFormulaErrorSeverity), typeof(FormulaErrorSeverities))]
[ExcludeFromCodeCoverage]
public abstract partial class FormulaErrorSeverities : TypeCollectionBase<FormulaErrorSeverityBase, IFormulaErrorSeverity> { }
