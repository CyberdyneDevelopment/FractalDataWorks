namespace Fdw.Abstractions;

/// <summary>
/// What every service has: an identity, a category, and whether it is usable.
/// </summary>
/// <remarks>
/// Split out of <see cref="IGenericService"/> because a command surface is not universal. A logging
/// pipeline, a host's request-pipeline settings and a telemetry exporter are all resolved, named
/// services that do real work, and none of them executes commands — before this split each had to
/// implement <c>Execute</c> twice and fail, which reported a capability the service never had.
/// <para>
/// A service that does take commands implements <see cref="IGenericService"/>, which adds them.
/// </para>
/// </remarks>
public interface IPlatformService
{
    /// <summary>Gets the identifier of this service instance.</summary>
    string Id { get; }

    /// <summary>Gets the service category this instance belongs to.</summary>
    string ServiceType { get; }

    /// <summary>Gets a value indicating whether the service is usable.</summary>
    bool IsAvailable { get; }
}
