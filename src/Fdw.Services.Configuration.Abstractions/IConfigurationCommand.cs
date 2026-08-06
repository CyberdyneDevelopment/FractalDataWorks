using Fdw.Commands.Data.Abstractions;

namespace Fdw.Services.Configuration;

/// <summary>
/// Marker interface for configuration commands — commands that target configuration data
/// (per-domain ConfigurationDb schemas, e.g. conn/sec/sched) with IsCurrent/IsDeleted filter and
/// cascade child loading baked in. Extends IDataCommand.
/// </summary>
public interface IConfigurationCommand : IDataCommand
{
}
