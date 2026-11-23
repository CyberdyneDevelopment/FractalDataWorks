using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data;

namespace Fdw.Services.Etl.Execution;

/// <summary>
/// A row of <c>pipe.RunGrant</c> (FDW-6144), as the configuration gateway reads and writes it. It carries no tenant: the table's default takes it from the caller's session, and the row-level security of
/// the tenant filters what is read.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed class RunGrantRecord
{
    /// <summary>Gets or sets the grant's id, the one logical key the configuration save and retire path uses.</summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>Gets or sets the person the runs act as.</summary>
    public Guid PersonId { get; set; }

    /// <summary>Gets or sets when the grant ends.</summary>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>Gets or sets a value indicating whether this is the current version of the row.</summary>
    public bool IsCurrent { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether the row is retired: the grant was revoked.</summary>
    public bool IsDeleted { get; set; }
}
