using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Data.Results;

/// <summary>
/// Invalid container name format.
/// </summary>
[TypeOption(typeof(DataServiceResultCodes), "InvalidContainerNameFormat", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class InvalidContainerNameFormatCode : DataServiceResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidContainerNameFormatCode"/> class.
    /// </summary>
    public InvalidContainerNameFormatCode()
        : base(21013, "InvalidContainerNameFormat", ResultSeverities.ByName("Error"),
            "Invalid container name format. Expected 'StoreName.Path', got '{ContainerName}'",
            isRetryable: false)
    {
    }
}