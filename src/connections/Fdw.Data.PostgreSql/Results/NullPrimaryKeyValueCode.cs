using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.PostgreSql.Results;

/// <summary>
/// Primary key value is null during update.
/// </summary>
[TypeOption(typeof(PostgreSqlDataResultCodes), "NullPrimaryKeyValue", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class NullPrimaryKeyValueCode : PostgreSqlDataResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NullPrimaryKeyValueCode"/> class.
    /// </summary>
    public NullPrimaryKeyValueCode()
        : base(21004, "NullPrimaryKeyValue",
            ResultSeverities.ByName("Error"),
            "Primary key field '{PrimaryKeyField}' has a null value",
            isRetryable: false)
    {
    }
}
