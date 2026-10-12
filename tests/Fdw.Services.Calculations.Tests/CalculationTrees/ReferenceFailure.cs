using System;

namespace Fdw.Services.Calculations.Tests.CalculationTrees;

/// <summary>
/// Thrown by the reference evaluator at the first failure, carrying the name of the result code the compiled calculation
/// must report for the same record.
/// </summary>
internal sealed class ReferenceFailure : Exception
{
    public ReferenceFailure(string code)
        : base(code)
    {
        Code = code;
    }

    public string Code { get; }
}
