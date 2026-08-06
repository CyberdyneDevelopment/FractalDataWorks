using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.DataContainers.Abstractions.ContainerWriteModeOptions;

/// <summary>
/// Update existing records based on key fields.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ContainerWriteModes), "Upsert", RestrictToCurrentCompilation = true)]
public sealed class UpsertWriteMode : ContainerWriteModeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpsertWriteMode"/> class.
    /// </summary>
    public UpsertWriteMode() : base(3, "Upsert", preservesExistingData: true, requiresExistingContainer: false, failsIfExists: false) { }
}
