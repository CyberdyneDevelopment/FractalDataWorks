using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.DataContainers.Abstractions.ContainerWriteModeOptions;

/// <summary>
/// Overwrite any existing data completely.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ContainerWriteModes), "Overwrite", RestrictToCurrentCompilation = true)]
public sealed class OverwriteWriteMode : ContainerWriteModeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OverwriteWriteMode"/> class.
    /// </summary>
    public OverwriteWriteMode() : base(0, "Overwrite", preservesExistingData: false, requiresExistingContainer: false, failsIfExists: false) { }
}
