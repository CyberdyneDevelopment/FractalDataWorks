using Fdw.Configuration;

namespace Fdw.Services.Hosts.Abstractions;

/// <summary>
/// The contract every host implementation's configuration satisfies.
/// </summary>
/// <remarks>
/// Deliberately carries no members. Each host option owns its own settings and declares them on its
/// own configuration record - Cors declares the CORS surface, SecurityHeaders declares its headers,
/// and so on. Enumerating every option's settings here would make one interface grow with every
/// option added, and would let an option see settings that are none of its business.
/// </remarks>
public interface IHostImplementationConfiguration : IImplementationConfiguration
{
}
