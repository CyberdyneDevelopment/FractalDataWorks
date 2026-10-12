using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.MsSql.Results;

/// <summary>Result code: an identifier supplied by configuration is not safe to put in a statement.</summary>
[TypeOption(typeof(MsSqlDataResultCodes), "InvalidIdentifier", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class InvalidIdentifierCode : MsSqlDataResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidIdentifierCode"/> class.
    /// </summary>
    public InvalidIdentifierCode()
        : base(21009, "InvalidIdentifier",
            ResultSeverities.ByName("Error"),
            "'{Identifier}' is not a safe name: only letters, digits and underscores are allowed",
            isRetryable: false)
    {
    }
}
