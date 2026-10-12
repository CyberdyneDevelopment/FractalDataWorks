using System;
using System.Collections.Generic;
using Fdw.Configuration;
using Fdw.Services.Calculations.Abstractions;

namespace Fdw.Services.Calculations.Tests.TestSupport;

/// <summary>
/// Minimal <see cref="ICalculationEntity"/> test double used to drive
/// <see cref="CalculationEntityTypeBase"/>-derived <c>ExecuteTyped</c>/<c>ValidateTypedConfiguration</c>
/// implementations without depending on the internal <c>CalculationEntity</c> record.
/// </summary>
internal sealed class TestCalculationEntity : ICalculationEntity
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string Name { get; init; } = "TestCalculation";

    public string? Description { get; init; }

    public string CalculationEntityType { get; init; } = "Formula";

    public string CalculationSource { get; init; } = "Default";

    public IReadOnlyList<CalculationInput> Inputs { get; init; } = [];

    public IReadOnlyList<IGenericConfiguration> Steps { get; init; } = [];

    public CalculationOutputSpec Output { get; init; } = new() { ResultFieldName = "Result" };

    public bool IsEnabled { get; init; } = true;

    public IGenericConfiguration? TypedConfiguration { get; init; }
}

/// <summary>
/// Test double whose <see cref="Name"/> getter throws on its first access only, so it can drive a
/// method's <c>catch</c> block without also blowing up the catch block's own logging call (which
/// re-reads <see cref="Name"/>).
/// </summary>
internal sealed class ThrowsOnceCalculationEntity : ICalculationEntity
{
    private int _nameAccessCount;

    public Guid Id { get; init; } = Guid.NewGuid();

    public string Name
    {
        get
        {
            _nameAccessCount++;
            return _nameAccessCount == 1
                ? throw new InvalidOperationException("Simulated failure reading Name")
                : "RecoveredName";
        }
    }

    public string? Description { get; init; }

    public string CalculationEntityType { get; init; } = "Formula";

    public string CalculationSource { get; init; } = "Default";

    public IReadOnlyList<CalculationInput> Inputs { get; init; } = [];

    public IReadOnlyList<IGenericConfiguration> Steps { get; init; } = [];

    public CalculationOutputSpec Output { get; init; } = new() { ResultFieldName = "Result" };

    public bool IsEnabled { get; init; } = true;

    public IGenericConfiguration? TypedConfiguration { get; init; }
}
