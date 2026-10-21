using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.MsSql.Translators;
using Microsoft.Data.SqlClient;

namespace Fdw.Data.MsSql;

/// <summary>
/// SQL Server's translators for the vectorized commands (FDW-1354), each named for the command it translates: the
/// connection finds one by the command's <c>CommandType</c>.
/// </summary>
[TypeCollection(typeof(MsSqlVectorizedDataCommandTranslatorBase), typeof(IVectorizedDataCommandTranslator<SqlCommand>), typeof(MsSqlVectorizedDataCommandTranslators))]
[ExcludeFromCodeCoverage]
public abstract partial class MsSqlVectorizedDataCommandTranslators :
    TypeCollectionBase<MsSqlVectorizedDataCommandTranslatorBase, IVectorizedDataCommandTranslator<SqlCommand>>
{
}
