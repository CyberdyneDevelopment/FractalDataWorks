using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Clients.Models;

/// <summary>Calculation step.</summary>
[TypeOption(typeof(DataflowNodeTypes), "Calculation")]
[ExcludeFromCodeCoverage]
public sealed class CalculationDataflowNodeType : DataflowNodeTypeBase
{
    /// <summary>Initializes a new instance of <see cref="CalculationDataflowNodeType"/>.</summary>
    public CalculationDataflowNodeType() : base(4, "Calculation") { }
}
