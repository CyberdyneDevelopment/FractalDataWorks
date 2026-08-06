using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Connections.MsSql.Limits;

/// <summary>
/// Subtype configuration for MaxResultSize limit entries on MsSql connections.
/// Maps to <c>conn.MsSqlMaxResultSize</c>. is <c>conn.MsSqlConnectionLimit</c>.
///
/// Caps the result set returned by a single query. MaxRows caps the Paging.Take
/// parameter on the command. MaxBytes caps the estimated byte footprint of the
/// result (rows × average row size). If a command requests more than the cap,
/// FDW enforces the cap and logs a warning; it does NOT reject the command.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration( ServiceCategory = "Connection",
    ServiceType = "MsSql")]
public sealed partial class MsSqlMaxResultSizeConfiguration
{
    /// <summary>Gets or sets the parent limit header identifier.</summary>
    public Guid MsSqlConnectionLimitId { get; set; }


    /// <summary>
    /// Gets or sets the maximum number of rows returned per query.
    /// When null, no row cap is enforced.
    /// </summary>
    public int? MaxRows { get; set; }

    /// <summary>
    /// Gets or sets the maximum estimated byte size of the result set.
    /// When null, no byte cap is enforced.
    /// </summary>
    public long? MaxBytes { get; set; }
}
