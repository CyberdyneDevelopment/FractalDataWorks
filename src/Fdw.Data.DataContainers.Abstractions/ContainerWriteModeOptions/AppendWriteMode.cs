using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.DataContainers.Abstractions.ContainerWriteModeOptions;

/// <summary>
/// Append new data to existing data.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ContainerWriteModes), "Append", RestrictToCurrentCompilation = true)]
public sealed class AppendWriteMode : ContainerWriteModeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AppendWriteMode"/> class.
    /// </summary>
    public AppendWriteMode() : base(1, "Append", preservesExistingData: true, requiresExistingContainer: false, failsIfExists: false) { }
}
