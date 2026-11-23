using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data;

namespace Fdw.Services.Etl.Execution;

/// <summary>
/// A row of <c>pipe.PipelineOffer</c> (FDW-6135), as the configuration gateway reads and writes it. It carries no tenant: the table's default takes it from the caller's session, and the row-level
/// security of the tenant filters what is read, as it does for every configuration row.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed class PipelineOfferRecord
{
    /// <summary>Gets or sets the offer's id, the one logical key the configuration save and retire path uses.</summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>Gets or sets the pipeline offered.</summary>
    public Guid PipelineId { get; set; }

    /// <summary>Gets or sets the person it is offered to; empty with the role empty means the whole tenant.</summary>
    public Guid? PersonId { get; set; }

    /// <summary>Gets or sets the role whose holders it is offered to.</summary>
    public string? RoleName { get; set; }

    /// <summary>Gets or sets a value indicating whether this is the current version of the row.</summary>
    public bool IsCurrent { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether the row is retired.</summary>
    public bool IsDeleted { get; set; }
}
