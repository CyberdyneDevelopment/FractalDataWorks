using System;
using Fdw.Configuration;

namespace Fdw.Services.Settings.Configuration;

/// <summary>One page of a home page domain: the page a scope holds in one state (FDW-2325).</summary>
public interface IHomePageImplementationConfiguration : IImplementationConfiguration
{
    /// <summary>Gets or sets the tenant the page is in.</summary>
    Guid TenantId { get; set; }

    /// <summary>Gets or sets the kind of scope that holds the page: <see cref="HomePageKinds"/>.</summary>
    string ScopeKind { get; set; }

    /// <summary>Gets or sets the id of the organization, tenant or person that holds the page.</summary>
    Guid ScopeId { get; set; }

    /// <summary>Gets or sets the state of the page: <see cref="HomePageStates"/>.</summary>
    string State { get; set; }

    /// <summary>Gets or sets the page as the JSON the console wrote it.</summary>
    string Body { get; set; }
}
