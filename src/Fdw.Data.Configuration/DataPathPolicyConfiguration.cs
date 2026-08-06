using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Connections;

/// <summary>
/// Configuration class for path authorization policies attached to a <see cref="DataPathConfiguration"/>.
/// Maps to the <c>data.DataPathPolicy</c> table.
/// </summary>
/// <remarks>
/// <para>
/// Each DataPath carries zero or more policies. When a caller invokes
/// <c>IDataStore.Resolve</c>, the runtime looks up the first active policy whose
/// <see cref="PolicyName"/> matches a registered <c>IPathAuthorizationPolicy</c> in the
/// <c>PathAuthorizationPolicies</c> TypeCollection and evaluates it. If no policies are
/// present the DataStore must not fall back silently — callers receive an explicit failure.
/// </para>
/// <para>
/// The FK from this table to <c>data.DataPath</c> is physical: <c>DataPathRowId</c> joins
/// to <c>data.DataPath.RowId</c>. This means the parent must be looked up via Physical key.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "DataStore", ServiceType = "DataPathPolicy")]
public partial class DataPathPolicyConfiguration : IGenericConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataPathPolicyConfiguration"/> class.
    /// </summary>
    public DataPathPolicyConfiguration()
    {
    }

    /// <summary>
    /// Gets or sets the unique logical identifier for this policy row.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the parent DataPath's logical Id (FK to data.DataPath.Id). The physical DataPathRowId
    /// is DB-managed and invisible — the save translator resolves it from this Id on insert.
    /// </summary>
    public Guid DataPathId { get; set; }

    /// <summary>
    /// Gets or sets the display name for this policy entry.
    /// </summary>
    /// <remarks>
    /// Why: IGenericConfiguration requires Name. data.DataPathPolicy has no Name column —
    /// the record is identified by DataPathRowId + PolicyName. This property is [NotMapped]
    /// so the source generator does not emit a Name column in DDL.
    /// </remarks>
    [NotMapped]
    public string Name { get; set; } = string.Empty;


    /// <summary>
    /// Gets the section name for IOptions binding.
    /// </summary>
    public string SectionName => "DataPathPolicies";

    /// <summary>
    /// Gets the service type — always "DataStore" for child config of DataPath.
    /// </summary>
    public string ServiceType => "DataStore";

    /// <summary>
    /// Gets the service option type — always "DataPathPolicy".
    /// </summary>
    public string? ServiceOptionType => "DataPathPolicy";


    /// <summary>
    /// Gets or sets the name of the <c>IPathAuthorizationPolicy</c> to apply.
    /// Must match a TypeOption registered in <c>PathAuthorizationPolicies</c>
    /// (e.g., "TenantScoped", "AdminOnly", "PublicRead", "DenyAll").
    /// </summary>
    public string PolicyName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional tenant scope for this policy.
    /// When set, this policy applies only to requests whose <c>IRequestContext.TenantId</c>
    /// matches this value.
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this is the current (active) version of the row.
    /// </summary>
    public bool IsCurrent { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this row has been soft-deleted.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the row was created.
    /// </summary>
    public DateTimeOffset CreateDate { get; set; }

    /// <summary>
    /// Gets or sets the database user who created the row.
    /// </summary>
    public string CreateBy { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when the row was last modified.
    /// </summary>
    public DateTimeOffset ModifyDate { get; set; }

    /// <summary>
    /// Gets or sets the database user who last modified the row.
    /// </summary>
    public string ModifyBy { get; set; } = string.Empty;
}
