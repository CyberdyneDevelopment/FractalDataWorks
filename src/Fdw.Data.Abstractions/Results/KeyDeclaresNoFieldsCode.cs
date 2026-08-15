using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.Abstractions.Results;

/// <summary>
/// A container key was declared with zero <see cref="Fdw.Data.Abstractions.IContainerKeyField"/> entries.
/// </summary>
[TypeOption(typeof(ContainerKeyResultCodes), "KeyDeclaresNoFields", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class KeyDeclaresNoFieldsCode : ContainerKeyResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="KeyDeclaresNoFieldsCode"/> class.
    /// </summary>
    public KeyDeclaresNoFieldsCode()
        : base(21000, "KeyDeclaresNoFields", ResultSeverities.ByName("Error"),
            "Key '{KeyName}' on container '{ContainerName}' declares no fields",
            isRetryable: false)
    {
    }
}
