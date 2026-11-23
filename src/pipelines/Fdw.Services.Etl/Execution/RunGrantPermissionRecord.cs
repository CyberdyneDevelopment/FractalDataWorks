using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data;

namespace Fdw.Services.Etl.Execution;

/// <summary>A row of <c>pipe.RunGrantPermission</c> (FDW-6144): one permission a grant allows, as the configuration gateway reads and writes it.</summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed class RunGrantPermissionRecord
{
    /// <summary>Gets or sets the row's id, the one logical key the configuration save and retire path uses.</summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>Gets or sets the grant the permission belongs to.</summary>
    public Guid GrantId { get; set; }

    /// <summary>Gets or sets the permission allowed.</summary>
    public string Permission { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether this is the current version of the row.</summary>
    public bool IsCurrent { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether the row is retired.</summary>
    public bool IsDeleted { get; set; }
}
