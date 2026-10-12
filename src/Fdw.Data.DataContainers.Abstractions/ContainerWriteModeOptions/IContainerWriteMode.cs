using Fdw.Collections;

namespace Fdw.Data.DataContainers.Abstractions.ContainerWriteModeOptions;

/// <summary>
/// Interface for container write modes.
/// Extends ITypeOption to enable TypeCollection discovery.
/// </summary>
public interface IContainerWriteMode : ITypeOption<int, ContainerWriteModeBase>
{
    /// <summary>
    /// Gets a value indicating whether this mode preserves existing data.
    /// </summary>
    bool PreservesExistingData { get; }

    /// <summary>
    /// Gets a value indicating whether this mode requires the container to exist.
    /// </summary>
    bool RequiresExistingContainer { get; }

    /// <summary>
    /// Gets a value indicating whether this mode fails if container exists.
    /// </summary>
    bool FailsIfExists { get; }
}
