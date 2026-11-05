using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Tags.Configuration;

/// <summary>A tag as its list shows it: the four properties every domain has, and who owns it.</summary>
/// <remarks>Read from <c>tag.Tag</c> alone, in one query per page.</remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public partial class TagDomainConfiguration : DomainConfiguration
{
    /// <summary>Gets or sets whose tag this is: <c>Tenant</c>, <c>Organization</c> or <c>Workspace</c>.</summary>
    public string OwnerKind { get; set; } = string.Empty;

    /// <summary>Gets or sets the Id of the tenant, organization or workspace named by <see cref="OwnerKind"/>.</summary>
    public Guid OwnerId { get; set; }
}
