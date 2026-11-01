using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.Abstractions.Results;

/// <summary>
/// A container already has a Primary key declared; a second Primary key was rejected.
/// </summary>
[TypeOption(typeof(ContainerKeyResultCodes), "ContainerAlreadyHasPrimaryKey", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ContainerAlreadyHasPrimaryKeyCode : ContainerKeyResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContainerAlreadyHasPrimaryKeyCode"/> class.
    /// </summary>
    public ContainerAlreadyHasPrimaryKeyCode()
        : base(41000, "ContainerAlreadyHasPrimaryKey", ResultSeverities.ByName("Error"),
            "Container '{ContainerName}' already has a primary key",
            isRetryable: false)
    {
    }
}
