using System;

namespace Fdw.Operations.Abstractions.Audit;

/// <summary>
/// What a caller states about an administrative change it has made: the rest of the record (who, when, which tenant) is
/// taken from the caller's own authentication context, so a caller cannot record a change in someone else's name.
/// </summary>
public sealed class AdministrativeChangeEntry
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AdministrativeChangeEntry"/> class.
    /// </summary>
    /// <param name="changeKind">What was done, for example RoleAssigned or SettingLocked.</param>
    /// <param name="itemKind">What it was done to, for example UserRole or Setting.</param>
    /// <param name="itemId">Which one, as the item names itself.</param>
    /// <param name="level">The level the change was made at.</param>
    /// <param name="organizationId">The organization the change was made in; required at the Organization level, refused above it.</param>
    /// <param name="oldValue">The value before, or null when there was none.</param>
    /// <param name="newValue">The value after, or null when the change removed it.</param>
    /// <param name="reason">Why, when the person gave a reason.</param>
    public AdministrativeChangeEntry(
        string changeKind,
        string itemKind,
        string itemId,
        IAdministrativeLevel level,
        Guid? organizationId,
        string? oldValue,
        string? newValue,
        string? reason)
    {
        ChangeKind = changeKind;
        ItemKind = itemKind;
        ItemId = itemId;
        Level = level;
        OrganizationId = organizationId;
        OldValue = oldValue;
        NewValue = newValue;
        Reason = reason;
    }

    /// <summary>Gets what was done.</summary>
    public string ChangeKind { get; }

    /// <summary>Gets what it was done to.</summary>
    public string ItemKind { get; }

    /// <summary>Gets which one.</summary>
    public string ItemId { get; }

    /// <summary>Gets the level the change was made at.</summary>
    public IAdministrativeLevel Level { get; }

    /// <summary>Gets the organization the change was made in, when it was made at the Organization level.</summary>
    public Guid? OrganizationId { get; }

    /// <summary>Gets the value before.</summary>
    public string? OldValue { get; }

    /// <summary>Gets the value after.</summary>
    public string? NewValue { get; }

    /// <summary>Gets why, when a reason was given.</summary>
    public string? Reason { get; }
}
