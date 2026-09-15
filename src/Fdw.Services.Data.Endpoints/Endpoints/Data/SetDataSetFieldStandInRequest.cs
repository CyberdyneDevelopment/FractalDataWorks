using System;
using System.Collections.Generic;

namespace Fdw.Services.Data.Endpoints;

/// <summary>Sets a data set field's stand-in strategy.</summary>
/// <remarks>
/// One flat request shape covering every strategy's parameters, since only one strategy's fields
/// are populated per call — StandInStrategyExecutor reads only the ones the chosen Strategy needs
/// and validates the rest are absent-or-ignored, matching the parameter schema each strategy
/// publishes via GET /standin-strategies.
/// </remarks>
public class SetDataSetFieldStandInRequest
{
    /// <summary>Gets or sets the data set name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the field name within the data set.</summary>
    public string Field { get; set; } = string.Empty;

    /// <summary>Gets or sets the strategy to use.</summary>
    public string Strategy { get; set; } = string.Empty;

    /// <summary>Gets or sets the fraction of generated values that should be null, or null to never generate null.</summary>
    public decimal? NullPercent { get; set; }

    /// <summary>Gets or sets the field's own random seed, or null to inherit the dataverse's.</summary>
    public string? Seed { get; set; }

    /// <summary>Gets or sets FixedValue's literal value.</summary>
    public string? FixedValue { get; set; }

    /// <summary>Gets or sets Sequence's first value.</summary>
    public long? StartValue { get; set; }

    /// <summary>Gets or sets Sequence's step between values.</summary>
    public long? Increment { get; set; }

    /// <summary>Gets or sets NumericRange's lowest value.</summary>
    public decimal? MinValue { get; set; }

    /// <summary>Gets or sets NumericRange's highest value.</summary>
    public decimal? MaxValue { get; set; }

    /// <summary>Gets or sets NumericRange's decimal places, or null for whole numbers.</summary>
    public int? Decimals { get; set; }

    /// <summary>Gets or sets DateRange's earliest date.</summary>
    public DateTime? FromDate { get; set; }

    /// <summary>Gets or sets DateRange's latest date.</summary>
    public DateTime? ToDate { get; set; }

    /// <summary>Gets or sets RegexPattern's pattern.</summary>
    public string? Pattern { get; set; }

    /// <summary>Gets or sets WeightedPick's options.</summary>
    public List<WeightedPickValueRequest>? Values { get; set; }

    /// <summary>Gets or sets DrawFromDataSet's source data set id.</summary>
    public Guid? SourceDataSetId { get; set; }

    /// <summary>Gets or sets DrawFromDataSet's source field id.</summary>
    public Guid? SourceFieldId { get; set; }
}
