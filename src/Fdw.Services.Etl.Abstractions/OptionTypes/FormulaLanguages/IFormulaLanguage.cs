using Fdw.Collections;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>
/// Interface for formula language type options consumed by Calculate transforms.
/// </summary>
public interface IFormulaLanguage : ITypeOption<int, IFormulaLanguage>
{
    /// <summary>
    /// Gets whether this language is evaluated by the in-process built-in evaluator. When false, the
    /// runtime requires <c>ITransformContext.CalculationEngine</c> to be an <see cref="IExpressionEvaluator"/>.
    /// </summary>
    bool IsBuiltin { get; }
}
