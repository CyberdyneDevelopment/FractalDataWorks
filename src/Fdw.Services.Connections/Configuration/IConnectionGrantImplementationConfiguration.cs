using System;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Connections.Configuration;

/// <summary>The contract every ConnectionGrant implementation carries (FDW-1581).</summary>
public interface IConnectionGrantImplementationConfiguration : IImplementationConfiguration
{
    /// <summary>Gets or sets the tenant the connection is granted to.</summary>
    Guid GranteeTenantId { get; set; }

    /// <summary>Gets or sets the name of the granted connection.</summary>
    string ConnectionName { get; set; }

    /// <summary>Gets or sets what the connection is granted for (for example Documents).</summary>
    string Purpose { get; set; }

    /// <summary>Gets or sets the folder the grant is narrowed to; <see langword="null"/> grants the connection whole.</summary>
    string? FolderPath { get; set; }

    /// <summary>Gets or sets whether the tenant may register its own connections for the purpose.</summary>
    bool MayRegisterOwn { get; set; }

    /// <summary>Gets or sets whether this is the live version of the row.</summary>
    bool IsCurrent { get; set; }

    /// <summary>Gets or sets whether the row has been deleted.</summary>
    bool IsDeleted { get; set; }
}
