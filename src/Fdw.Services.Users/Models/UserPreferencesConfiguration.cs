using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Users.Models;

/// <summary>
/// Maps to <c>usr.UserPreferences</c> — user display/locale preferences.
/// One row per user (enforced by <c>UX_UserPreferences_UserId_Current</c>).
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "UserPreference")]
public sealed partial class UserPreferencesConfiguration : IGenericConfiguration
{
    /// <inheritdoc />
    // Why: UserPreferences has no business Name; UserId.ToString() satisfies the IGenericConfiguration
    // contract. The provider queries by UserId — never by Name.
    public Guid Id { get; set; }

    /// <inheritdoc />
    public string Name { get; set; } = string.Empty;

    /// <inheritdoc />
    public string SectionName => "UserPreferences";

    /// <inheritdoc />
    public string ServiceType => "UserPreference";

    /// <inheritdoc />
    public string? ServiceOptionType => null;


    /// <summary>Gets or sets the user this preference record belongs to.</summary>
    public Guid UserId { get; set; }

    /// <summary>Gets or sets the optional UI theme name.</summary>
    public string? ThemeName { get; set; }

    /// <summary>Gets or sets whether dark mode is enabled.</summary>
    public bool DarkMode { get; set; }

    /// <summary>Gets or sets the optional preferred language code.</summary>
    public string? Language { get; set; }

    /// <summary>Gets or sets the optional timezone identifier.</summary>
    public string? Timezone { get; set; }

    /// <summary>Gets or sets whether this is the current active version.</summary>
    public bool IsCurrent { get; set; } = true;

    /// <summary>Gets or sets whether this record has been soft-deleted.</summary>
    public bool IsDeleted { get; set; }

    /// <summary>Gets or sets the original creation date from the source system.</summary>
    public DateTimeOffset? SrcCreateDate { get; set; }

    /// <summary>Gets or sets the timestamp when the record was created.</summary>
    public DateTimeOffset CreateDate { get; set; }

    /// <summary>Gets or sets the database user who created the record.</summary>
    public string CreateBy { get; set; } = string.Empty;

    /// <summary>Gets or sets the application user on whose behalf the record was created.</summary>
    public string CreateOnBehalfOf { get; set; } = string.Empty;

    /// <summary>Gets or sets the timestamp when the record was last modified.</summary>
    public DateTimeOffset ModifyDate { get; set; }

    /// <summary>Gets or sets the database user who last modified the record.</summary>
    public string ModifyBy { get; set; } = string.Empty;

    /// <summary>Gets or sets the application user on whose behalf the record was last modified.</summary>
    public string ModifyOnBehalfOf { get; set; } = string.Empty;
}
