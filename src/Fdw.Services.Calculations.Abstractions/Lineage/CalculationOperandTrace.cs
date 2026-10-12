namespace Fdw.Services.Calculations.Abstractions.Lineage;

/// <summary>
/// Records where one operand of a calculation step got its value.
/// </summary>
/// <remarks>
/// This is the leaf of calculation traceability: it answers "this number came from HERE" for a
/// single operand — which source kind, which alias or field, and what value that resolved to at
/// execution time. Only the step executor can observe this, because the binding exists solely
/// inside the loop; reconstructing it afterwards would mean re-running the calculation.
/// </remarks>
public sealed class CalculationOperandTrace
{
    /// <summary>Gets the operand name, which is also the operation parameter it was bound to.</summary>
    public string OperandName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the operand's source kind — <c>Input</c>, <c>StepReference</c>, or <c>Literal</c>.
    /// Mirrors the configured operand type verbatim.
    /// </summary>
    public string SourceKind { get; init; } = string.Empty;

    /// <summary>
    /// Gets what the operand pointed at: the input alias, the prior step's output alias, or the
    /// literal text as configured. Empty only when the configuration supplied none.
    /// </summary>
    public string SourceReference { get; init; } = string.Empty;

    /// <summary>
    /// Gets the field narrowed out of the referenced value, when the operand named one;
    /// <see langword="null"/> when the whole referenced value was used.
    /// </summary>
    public string? FieldName { get; init; }

    /// <summary>Gets the value the operand actually resolved to and handed the operation.</summary>
    public CalculationTraceValue ResolvedValue { get; init; } = new();
}
