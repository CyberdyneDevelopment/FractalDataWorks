using System.Collections.Generic;
using Fdw.Commands.Data.Abstractions;
using Microsoft.Data.SqlClient;

namespace Fdw.Data.MsSql.Translators;

/// <summary>
/// Base class for SQL Server's vectorized command translators (FDW-1354).
/// </summary>
public abstract class MsSqlVectorizedDataCommandTranslatorBase : VectorizedDataCommandTranslatorBase<SqlCommand>, IStatesOperations
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MsSqlVectorizedDataCommandTranslatorBase"/> class.
    /// </summary>
    /// <param name="name">The command the translator translates (must match the <c>[TypeOption]</c> attribute).</param>
    protected MsSqlVectorizedDataCommandTranslatorBase(string name)
        : base(name)
    {
    }

    /// <inheritdoc />
    /// <remarks>Abstract on purpose: a vectorized translator added later does not compile until it states what its command performs (FDW-6682).</remarks>
    public abstract IReadOnlyList<string> Operations { get; }
}
