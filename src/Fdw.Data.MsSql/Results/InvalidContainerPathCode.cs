using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.MsSql.Results;

/// <summary>
/// Container path is not a DatabasePath.
/// </summary>
[TypeOption(typeof(MsSqlDataResultCodes), "InvalidContainerPath", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class InvalidContainerPathCode : MsSqlDataResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidContainerPathCode"/> class.
    /// </summary>
    public InvalidContainerPathCode()
        : base(20001, "InvalidContainerPath",
            ResultSeverities.ByName("Error"),
            "Container path must be a DatabasePath for MsSql translator",
            isRetryable: false)
    {
    }
}