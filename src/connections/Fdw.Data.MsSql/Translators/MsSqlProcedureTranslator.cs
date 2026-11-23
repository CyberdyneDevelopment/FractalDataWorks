using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Data.MsSql.Logging;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data.Builders;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Data.MsSql.Translators;

/// <summary>
/// Translates <see cref="IExecuteProcedureCommand"/> to a call of the stored procedure the container's database path names (FDW-6682).
/// </summary>
/// <remarks>
/// The command text is the procedure's quoted name and nothing else, run as a stored procedure call; every value is a bound parameter, so a value is never part of
/// a statement. Parameters are scalars (text, boolean, number, Guid, date) and null is stated, so a null is sent as <c>DBNull</c>; any other type is refused with a
/// coded message and is never turned into text. The container's type must state <c>Execute</c>: the connection asks the operation rule before it calls this translator,
/// and this translator asks it too, so a direct call is refused the same way.
/// </remarks>
[TypeOption(typeof(MsSqlDataCommandTranslators), "Execute", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class MsSqlProcedureTranslator : MsSqlDataCommandTranslatorBase
{
    private static readonly string[] Performed = ["Execute"];

    /// <summary>Initializes a new instance of the <see cref="MsSqlProcedureTranslator"/> class.</summary>
    public MsSqlProcedureTranslator()
        : base("Execute")
    {
    }

    /// <inheritdoc />
    /// <remarks>A procedure call is an execution and nothing else; no read or write is assumed from it.</remarks>
    public override IReadOnlyList<string> Operations => Performed;

    /// <inheritdoc />
    public override Task<IGenericResult<SqlCommand>> Translate(IDataCommand command, IStorageContainer container, CancellationToken cancellationToken = default)
        => Task.FromResult(Build(command, container));

    private static IGenericResult<SqlCommand> Build(IDataCommand command, IStorageContainer container)
    {
        var logger = NullLogger<MsSqlProcedureTranslator>.Instance;
        if (command is not IExecuteProcedureCommand call)
            return GenericResult<SqlCommand>.Failure(MsSqlTranslatorLog.InvalidCommandType(logger, nameof(MsSqlProcedureTranslator), nameof(IExecuteProcedureCommand), command.GetType().Name));

        var operations = DataOperationRule.Check(container, Performed, logger);
        if (operations.IsFailure)
            return operations.ToNewResult<SqlCommand>();

        if (container.Path is not IDatabasePath path)
            return GenericResult<SqlCommand>.Failure(MsSqlProcedureTranslatorLog.ContainerPathNotDatabase(logger, container.Name, container.Path.GetType().Name));

        var procedure = BuildSchemaQualifiedTableName(path);
        var native = new SqlCommand(procedure) { CommandType = CommandType.StoredProcedure };
        foreach (var (name, value) in call.Parameters)
        {
            if (!IsValidColumnName(name))
                return GenericResult<SqlCommand>.Failure(MsSqlProcedureTranslatorLog.ParameterNameNotIdentifier(logger, procedure, name));

            if (value is not null && !IsScalar(value))
                return GenericResult<SqlCommand>.Failure(MsSqlProcedureTranslatorLog.ParameterNotScalar(logger, procedure, name, value.GetType().Name));

            AddParameter(native, name, value);
        }

        MsSqlProcedureTranslatorLog.CallBuilt(logger, procedure, native.Parameters.Count);
        return GenericResult<SqlCommand>.Success(native);
    }

    private static bool IsScalar(object value)
        => value is string or bool or byte or short or int or long or float or double or decimal or Guid or DateTime or DateTimeOffset or TimeSpan;
}
