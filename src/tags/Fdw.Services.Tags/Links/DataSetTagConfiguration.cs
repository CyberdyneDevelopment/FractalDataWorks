using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Tags.Links;

/// <summary>One row of <c>data.DataSetTag</c>: a DataSet carries a tag. It holds its own Id, the two Ids and the platform columns, nothing else.</summary>
/// <remarks>
/// Property names are the column names. The row is never changed: tagging inserts it, and removing the tag retires it (<see cref="IsCurrent"/> false,
/// <see cref="IsDeleted"/> true).
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed partial class DataSetTagConfiguration : IGenericConfiguration
{
    /// <summary>Gets or sets the link's own durable Id.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the Id of the DataSet that carries the tag.</summary>
    public Guid DataSetId { get; set; }

    /// <summary>Gets or sets the Id of the tag.</summary>
    public Guid TagId { get; set; }

    /// <summary>Gets or sets the tenant of the row.</summary>
    public Guid? TenantId { get; set; }

    /// <summary>Gets or sets the row-level visibility group.</summary>
    public Guid? VisibilityGroupId { get; set; }

    /// <summary>Gets or sets whether this is the current version of the row.</summary>
    public bool IsCurrent { get; set; } = true;

    /// <summary>Gets or sets whether the row has been retired.</summary>
    public bool IsDeleted { get; set; }

    /// <summary>Gets or sets the original creation date from the source system.</summary>
    public DateTimeOffset? SrcCreateDate { get; set; }

    /// <summary>Gets or sets the timestamp when the row was created.</summary>
    public DateTimeOffset CreateDate { get; set; }

    /// <summary>Gets or sets the database user who created the row.</summary>
    public string CreateBy { get; set; } = string.Empty;

    /// <summary>Gets or sets the application user on whose behalf the row was created.</summary>
    public string CreateOnBehalfOf { get; set; } = string.Empty;

    /// <summary>Gets or sets the timestamp when the row was last modified.</summary>
    public DateTimeOffset ModifyDate { get; set; }

    /// <summary>Gets or sets the database user who last modified the row.</summary>
    public string ModifyBy { get; set; } = string.Empty;

    /// <summary>Gets or sets the application user on whose behalf the row was last modified.</summary>
    public string ModifyOnBehalfOf { get; set; } = string.Empty;
}
