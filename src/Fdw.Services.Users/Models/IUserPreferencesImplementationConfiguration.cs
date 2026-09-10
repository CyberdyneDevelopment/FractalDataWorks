using Fdw.Configuration;
using Fdw.Data;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Users.Models;

/// <summary>The contract every UserPreferences implementation carries.</summary>
public interface IUserPreferencesImplementationConfiguration : IImplementationConfiguration
{
    /// <summary>Gets or sets the UserPreferences UserId.</summary>
    Guid UserId { get; set; }

    /// <summary>Gets or sets the UserPreferences ThemeName.</summary>
    string? ThemeName { get; set; }

    /// <summary>Gets or sets the UserPreferences DarkMode.</summary>
    bool DarkMode { get; set; }

    /// <summary>Gets or sets the UserPreferences Language.</summary>
    string? Language { get; set; }

    /// <summary>Gets or sets the UserPreferences Timezone.</summary>
    string? Timezone { get; set; }

    /// <summary>Gets or sets the UserPreferences IsCurrent.</summary>
    bool IsCurrent { get; set; }

    /// <summary>Gets or sets the UserPreferences IsDeleted.</summary>
    bool IsDeleted { get; set; }

    /// <summary>Gets or sets the UserPreferences SrcCreateDate.</summary>
    DateTimeOffset? SrcCreateDate { get; set; }

    /// <summary>Gets or sets the UserPreferences CreateDate.</summary>
    DateTimeOffset CreateDate { get; set; }

    /// <summary>Gets or sets the UserPreferences CreateBy.</summary>
    string CreateBy { get; set; }

    /// <summary>Gets or sets the UserPreferences CreateOnBehalfOf.</summary>
    string CreateOnBehalfOf { get; set; }

    /// <summary>Gets or sets the UserPreferences ModifyDate.</summary>
    DateTimeOffset ModifyDate { get; set; }

    /// <summary>Gets or sets the UserPreferences ModifyBy.</summary>
    string ModifyBy { get; set; }

    /// <summary>Gets or sets the UserPreferences ModifyOnBehalfOf.</summary>
    string ModifyOnBehalfOf { get; set; }
}
