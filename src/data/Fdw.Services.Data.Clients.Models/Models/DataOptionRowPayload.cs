using System;

namespace Fdw.Services.Data.Clients.Models;

/// <summary>One option row of a data store container column (FDW-5664).</summary>
public sealed class DataOptionRowPayload
{
    /// <summary>Gets or sets the row's id, by which it is changed and removed.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the column.</summary>
    public string Column { get; set; } = string.Empty;

    /// <summary>Gets or sets the value the option applies to.</summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>Gets or sets the option type (a name <c>GET /data-option-types</c> answers).</summary>
    public string OptionType { get; set; } = string.Empty;

    /// <summary>Gets or sets the option (a name of its option type's options).</summary>
    public string Option { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the row is switched on.</summary>
    public bool Active { get; set; }
}
