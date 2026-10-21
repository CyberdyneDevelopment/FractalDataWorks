using Fdw.Commands.Data.Abstractions;
using Microsoft.Data.SqlClient;

namespace Fdw.Data.MsSql.Translators;

/// <summary>
/// Base class for SQL Server's vectorized command translators (FDW-1354).
/// </summary>
public abstract class MsSqlVectorizedDataCommandTranslatorBase : VectorizedDataCommandTranslatorBase<SqlCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MsSqlVectorizedDataCommandTranslatorBase"/> class.
    /// </summary>
    /// <param name="name">The command the translator translates (must match the <c>[TypeOption]</c> attribute).</param>
    protected MsSqlVectorizedDataCommandTranslatorBase(string name)
        : base(name)
    {
    }
}
