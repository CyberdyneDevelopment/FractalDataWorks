using System;
using Fdw.Configuration;

namespace Fdw.Services.Tags.Configuration;

/// <summary>The contract every tag implementation carries.</summary>
/// <remarks>
/// A tag's name and owner live on the domain row (<see cref="TagDomainConfiguration"/>), because a list of tags shows them. The implementation
/// row holds what only the tag itself carries.
/// </remarks>
public interface ITagImplementationConfiguration : IImplementationConfiguration
{
    /// <summary>Gets or sets what the tag means, shown beside its name.</summary>
    string? Description { get; set; }

    /// <summary>Gets or sets whose tag this is: <c>Tenant</c>, <c>Organization</c> or <c>Workspace</c>. A list field: it is written to the domain row.</summary>
    string OwnerKind { get; set; }

    /// <summary>Gets or sets the Id of the tenant, organization or workspace named by <see cref="OwnerKind"/>. A list field: it is written to the domain row.</summary>
    Guid OwnerId { get; set; }
}
