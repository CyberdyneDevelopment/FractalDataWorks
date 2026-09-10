using System;
using System.Collections.Generic;
using Fdw.Services.Calculations.Configuration;

namespace Fdw.Services.Calculations.Tests.TestSupport;

/// <summary>
/// A calculation configuration whose type no <see cref="CalculationEntityTypes"/> option claims, so
/// the kind it maps back to is unknown. That is the only way an unknown kind can now be expressed:
/// the record's TYPE is what names the kind, not a string on the row.
/// </summary>
internal sealed class UnregisteredCalculationConfiguration : ICalculationEntityImplementationConfiguration
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Domain { get; set; } = string.Empty;

    public string Implementation { get; set; } = string.Empty;

    public string CalculationSource { get; set; } = "Configuration";

    public string? Description { get; set; }

    public string? OutputDataSetName { get; set; }

    public string? ResultFieldName { get; set; }

    public string ResultDataTypeName { get; set; } = "Decimal";

    public bool IsEnabled { get; set; } = true;

    public IList<CalculationEntityInputRecord> Inputs { get; set; } = [];

    public IList<CalculationStepConfiguration> Steps { get; set; } = [];
}
