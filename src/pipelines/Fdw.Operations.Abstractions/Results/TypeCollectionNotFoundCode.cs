using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Operations.Abstractions.Results;

/// <summary>
/// No loaded assembly declares a TypeCollection with the requested name.
/// </summary>
[TypeOption(typeof(OperationsResultCodes), "TypeCollectionNotFound", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class TypeCollectionNotFoundCode : OperationsResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TypeCollectionNotFoundCode"/> class.
    /// </summary>
    public TypeCollectionNotFoundCode()
        : base(
            31002,
            "TypeCollectionNotFound",
            ResultSeverities.ByName("Error"),
            "TypeCollection '{CollectionName}' was not found among the loaded collections",
            isRetryable: false)
    {
    }
}
