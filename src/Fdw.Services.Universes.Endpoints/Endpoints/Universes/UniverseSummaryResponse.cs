using System;
using Fdw.Web.Endpoints.Contracts;

namespace Fdw.Services.Universes.Endpoints;

/// <summary>A universe as it appears in a list.</summary>
/// <remarks>
/// Carries both Id and Name. Routes key on name because names make better URLs; a client keys its
/// cache on Id so a rename is not simultaneously a broken link and a cache miss.
/// </remarks>
public class UniverseSummaryResponse : ResourceSummary
{
    /// <summary>Gets or sets the durable logical identity.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the optional display name.</summary>
    public string? DisplayName { get; set; }

    /// <summary>Gets or sets the optional description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the lifecycle status.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Gets or sets who can find this project.</summary>
    public string Visibility { get; set; } = string.Empty;

    /// <summary>Gets or sets what happens when someone asks to join.</summary>
    public string JoinPolicy { get; set; } = string.Empty;

    /// <summary>Gets or sets the owning user.</summary>
    public Guid OwnerUserId { get; set; }

    /// <summary>Gets or sets how many members the project has.</summary>
    public int MemberCount { get; set; }

    /// <summary>Gets or sets how many resources are attached.</summary>
    public int ResourceCount { get; set; }

    /// <summary>Gets or sets when the project was created.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Gets or sets when the project was last modified.</summary>
    public DateTimeOffset ModifiedAt { get; set; }
}
