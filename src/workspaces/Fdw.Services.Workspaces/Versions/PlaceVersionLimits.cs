namespace Fdw.Services.Workspaces;

/// <summary>
/// The limit on a read of the versions of one definition held at places (FDW-7104): how many versions one read may hold.
/// </summary>
/// <remarks>
/// Configuration, never code: the default is the place service type option's declared <c>MaxVersions</c>, a host changes it at registration with
/// <c>ServiceTypeDefaults.Override</c>, and a value stated under <c>Places:Default</c> wins. A definition holds at most one version per place.
/// </remarks>
public sealed class PlaceVersionLimits
{
    /// <summary>Initializes a new instance of the <see cref="PlaceVersionLimits"/> class.</summary>
    /// <param name="maxVersions">The most versions of one definition one read holds.</param>
    public PlaceVersionLimits(int maxVersions)
    {
        MaxVersions = maxVersions;
    }

    /// <summary>Gets the most versions of one definition one read holds.</summary>
    public int MaxVersions { get; }
}
