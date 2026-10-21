using System;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Connections.Configuration;

/// <summary>The contract every ConnectionAssignment implementation carries (FDW-1581).</summary>
public interface IConnectionAssignmentImplementationConfiguration : IImplementationConfiguration
{
    /// <summary>Gets or sets the organization the connection is assigned to.</summary>
    Guid OrganizationId { get; set; }

    /// <summary>Gets or sets what the connection is assigned for.</summary>
    string Purpose { get; set; }

    /// <summary>Gets or sets the logical Id of the grant the assignment uses.</summary>
    Guid GrantId { get; set; }

    /// <summary>Gets or sets the folder inside the grant's folder the organization is narrowed to; <see langword="null"/> uses the grant as it is.</summary>
    string? BaseFolder { get; set; }

    /// <summary>Gets or sets whether this is the live version of the row.</summary>
    bool IsCurrent { get; set; }

    /// <summary>Gets or sets whether the row has been deleted.</summary>
    bool IsDeleted { get; set; }
}
