using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Calculations.Results;

/// <summary>
/// Use the GetData overload with IDataCommand for query execution.
/// </summary>
[TypeOption(typeof(CalculationResultCodes), "UseDataCommandOverload")]
[ExcludeFromCodeCoverage]
public sealed class UseDataCommandOverloadCode : CalculationResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UseDataCommandOverloadCode"/> class.
    /// </summary>
    public UseDataCommandOverloadCode()
        : base(21006, "UseDataCommandOverload",
            ResultSeverities.ByName("Error"),
            "Use GetData<TData>(IDataCommand) overload for query execution",
            isRetryable: false)
    {
    }
}