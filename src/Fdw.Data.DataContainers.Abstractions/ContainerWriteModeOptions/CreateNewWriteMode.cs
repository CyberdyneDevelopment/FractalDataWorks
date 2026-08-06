using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.DataContainers.Abstractions.ContainerWriteModeOptions;

/// <summary>
/// Get new container, fail if it already exists.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ContainerWriteModes), "CreateNew", RestrictToCurrentCompilation = true)]
public sealed class CreateNewWriteMode : ContainerWriteModeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateNewWriteMode"/> class.
    /// </summary>
    public CreateNewWriteMode() : base(2, "CreateNew", preservesExistingData: false, requiresExistingContainer: false, failsIfExists: true) { }
}
