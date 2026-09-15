using System;
using System.Collections.Generic;

namespace Fdw.Services.Data.Endpoints;

/// <summary>A field's stand-in, as it stands after a set or a read.</summary>
public class DataSetFieldStandInResponse
{
    /// <summary>Gets or sets the field's own logical id.</summary>
    public Guid FieldId { get; set; }

    /// <summary>Gets or sets the field name.</summary>
    public string Field { get; set; } = string.Empty;

    /// <summary>Gets or sets the active strategy.</summary>
    public string Strategy { get; set; } = string.Empty;

    /// <summary>Gets or sets the fraction of generated values that should be null, or null to never generate null.</summary>
    public decimal? NullPercent { get; set; }

    /// <summary>Gets or sets the field's own random seed, or null to inherit the dataverse's.</summary>
    public string? Seed { get; set; }

    /// <summary>Gets or sets the strategy's own parameters, by name.</summary>
    public IDictionary<string, object?> Parameters { get; set; } = new Dictionary<string, object?>(StringComparer.Ordinal);
}
