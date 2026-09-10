using Fdw.Configuration;
using Fdw.Configuration.Abstractions;
using Fdw.Data;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Authorization.Configuration;

/// <summary>The contract every Permission implementation carries.</summary>
public interface IPermissionImplementationConfiguration : IImplementationConfiguration
{
    /// <summary>Gets or sets the domain record\'s durable id.</summary>
    Guid PermissionId { get; set; }

    /// <summary>Gets or sets the Permission PermissionDomain.</summary>
    string PermissionDomain { get; set; }

    /// <summary>Gets or sets the Permission Resource.</summary>
    string Resource { get; set; }

    /// <summary>Gets or sets the Permission Action.</summary>
    string Action { get; set; }

    /// <summary>Gets or sets the Permission Scope.</summary>
    string Scope { get; set; }

    /// <summary>Gets or sets the Permission DisplayName.</summary>
    string? DisplayName { get; set; }

    /// <summary>Gets or sets the Permission Description.</summary>
    string? Description { get; set; }

    /// <summary>Gets or sets the Permission Category.</summary>
    string? Category { get; set; }

    /// <summary>Gets or sets the Permission SortOrder.</summary>
    int SortOrder { get; set; }
}
