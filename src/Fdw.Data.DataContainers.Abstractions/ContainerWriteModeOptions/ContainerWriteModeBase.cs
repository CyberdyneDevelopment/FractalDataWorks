using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.Data.DataContainers.Abstractions.ContainerWriteModeOptions;

/// <summary>
/// Base class for container write modes.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption base class - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
public abstract class ContainerWriteModeBase : TypeOptionBase<ContainerWriteModeBase>, IContainerWriteMode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContainerWriteModeBase"/> class.
    /// </summary>
    /// <param name="name">The name of this container write mode.</param>
    /// <param name="preservesExistingData">Whether this mode preserves existing data.</param>
    /// <param name="requiresExistingContainer">Whether this mode requires the container to exist.</param>
    /// <param name="failsIfExists">Whether this mode fails if container exists.</param>
    protected ContainerWriteModeBase(string name, bool preservesExistingData, bool requiresExistingContainer, bool failsIfExists)
        : base(name)
    {
        PreservesExistingData = preservesExistingData;
        RequiresExistingContainer = requiresExistingContainer;
        FailsIfExists = failsIfExists;
    }

    /// <inheritdoc />
    public bool PreservesExistingData { get; }

    /// <inheritdoc />
    public bool RequiresExistingContainer { get; }

    /// <inheritdoc />
    public bool FailsIfExists { get; }
}
