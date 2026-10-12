using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Connections.MsSql.Results;

/// <summary>
/// No POCO mapper found for the requested type.
/// </summary>
[TypeOption(typeof(MsSqlResultCodes), "NoMapperFound", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class NoMapperFoundCode : MsSqlResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NoMapperFoundCode"/> class.
    /// </summary>
    public NoMapperFoundCode()
        : base(
            31000,
            "NoMapperFound",
            ResultSeverities.ByName("Error"),
            "No POCO mapper found for type '{TypeName}'. Add [GenerateMapper] attribute to the type or create a manual PocoMapperBase implementation.",
            isRetryable: false)
    {
    }
}
