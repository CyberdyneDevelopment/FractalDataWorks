using Fdw.Configuration;

namespace Fdw.Services.Configuration;

/// <summary>
/// Declares that the members of a domain provider are placed when they are made (FDW-3692). A provider that does not implement this
/// places nothing: the declaration is explicit, so a domain is never placed by accident.
/// </summary>
/// <typeparam name="TImplementationConfiguration">The domain's implementation contract.</typeparam>
public interface IPlacedConfigurationProvider<in TImplementationConfiguration>
    where TImplementationConfiguration : IImplementationConfiguration
{
    /// <summary>Gets the placement that places this provider's members.</summary>
    IConfigurationPlacement Placement { get; }

    /// <summary>Gets what this provider's members are, as the placement names it.</summary>
    string PlacementKind { get; }

    /// <summary>Gets whether a member being made still needs placing: false for one that already has an owner of its own.</summary>
    /// <param name="record">The member being made.</param>
    /// <returns>True when the member is to be placed.</returns>
    bool Needs(TImplementationConfiguration record);
}
