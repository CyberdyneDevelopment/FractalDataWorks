using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Services.Calculations.Abstractions;

namespace Fdw.Services.Calculations;

/// <summary>
/// Mutable registry of calculation entity types.
/// Supports cross-assembly registration of types decorated with [TypeOption(typeof(CalculationEntityTypes), ...)].
/// </summary>
/// <remarks>
/// Uses [MutableTypeCollection] to allow calculation implementations in separate assemblies to
/// register their types at startup without modifying this class.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(CalculationEntityTypeBase), typeof(ICalculationEntityType), typeof(CalculationEntityTypes))]
public abstract partial class CalculationEntityTypes : TypeCollectionBase<CalculationEntityTypeBase, ICalculationEntityType>
{
    /// <summary>
    /// The service category used for configuration section binding.
    /// Configuration keys follow the pattern: Calculations:{TypeName}:{index}:{Property}
    /// </summary>
    public static string ServiceCategory => "Calculation";
}
