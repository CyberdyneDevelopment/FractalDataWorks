using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data.Abstractions;
using Fdw.Commands.Data.Executions;
using Fdw.Commands.Data.ValueSources;
using Fdw.Data.Abstractions;
using Fdw.Data.MsSql.Logging;
using Fdw.Data.MsSql.Results;
using Fdw.Data.MsSql.ValueSources;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Data.MsSql.Translators;

/// <summary>
/// Translates <see cref="IExecuteCommand"/> to an <c>EXEC</c> of the container it is dispatched with. On SQL Server the
/// executable container type is a stored procedure; that mapping lives only here.
/// </summary>
/// <remarks>
/// The container comes from the data store's own container metadata, so its name is quoted by the dialect and never
/// built from caller text. Each parameter must be a field of the container; each value is a parameter of the batch
/// (a constant) or a variable set from the database clock (the current timestamp).
/// </remarks>
[TypeOption(typeof(MsSqlDataCommandTranslators), "Execute", RestrictToCurrentCompilation = true)]
public sealed class MsSqlExecuteTranslator : MsSqlDataCommandTranslatorBase
{
    private const string ExecutableContainerType = "StoredProcedure";

    /// <summary>
    /// Initializes a new instance of the <see cref="MsSqlExecuteTranslator"/> class.
    /// </summary>
    public MsSqlExecuteTranslator()
        : base("Execute")
    {
    }

    /// <summary>
    /// Translates an execute command into an EXEC statement. Returns a failure result with a reason when the command
    /// cannot be translated.
    /// </summary>
    public override Task<IGenericResult<SqlCommand>> Translate(
        IDataCommand command,
        IStorageContainer container,
        CancellationToken cancellationToken = default)
    {
        var logger = NullLogger<MsSqlExecuteTranslator>.Instance;
        var containerName = container is null ? "<null>" : container.Name;
        MsSqlExecuteTranslatorLog.Translating(logger, containerName);

        try
        {
            return Task.FromResult(TranslateCommand(command, container, containerName));
        }
        catch (Exception ex)
        {
            MsSqlExecuteTranslatorLog.TranslationFailed(logger, ex, containerName, ex.Message);
            return Task.FromResult(GenericResult<SqlCommand>.Failure(
                MsSqlDataResultCodes.ByName("ExecuteTranslationFailed"),
                ResultDetails.Create("ErrorMessage", ex.Message)));
        }
    }

    private static IGenericResult<SqlCommand> TranslateCommand(IDataCommand command, IStorageContainer? container, string containerName)
    {
        var logger = NullLogger<MsSqlExecuteTranslator>.Instance;
        if (container is null)
        {
            MsSqlExecuteTranslatorLog.ContainerNull(logger);
            return GenericResult<SqlCommand>.Failure(MsSqlDataResultCodes.ByName("ContainerNull"));
        }

        if (container.Path is not IDatabasePath path)
        {
            MsSqlExecuteTranslatorLog.InvalidContainerPath(logger, containerName);
            return GenericResult<SqlCommand>.Failure(MsSqlDataResultCodes.ByName("InvalidContainerPath"));
        }

        if (command is not IExecuteCommand execute)
        {
            return GenericResult<SqlCommand>.Failure(
                MsSqlTranslatorLog.InvalidCommandType(logger, "MsSqlExecuteTranslator", nameof(IExecuteCommand), command.GetType().Name));
        }

        var problem = Problem(execute.Options, container);
        if (problem is not null)
            return Refused(containerName, problem);

        var statement = Statement(execute.Options, container, path);
        if (statement.Problem is not null)
            return Refused(containerName, statement.Problem);

        MsSqlExecuteTranslatorLog.Translated(logger, containerName, execute.Options.Parameters.Count);
        return GenericResult<SqlCommand>.Success(statement.Command);
    }

    private static string? Problem(ExecuteOptions options, IStorageContainer container)
    {
        if (!string.Equals(container.ContainerType.Name, ExecutableContainerType, StringComparison.OrdinalIgnoreCase))
            return $"container '{container.Name}' is a {container.ContainerType.Name}; this connection executes only a {ExecutableContainerType}";

        var general = options.FirstProblem();
        if (general is not null)
            return general;

        foreach (var parameter in options.Parameters)
        {
            if (!MsSqlIdentifiers.IsSafe(parameter.Name))
                return $"'{parameter.Name}' is not a safe parameter name (letters, digits and underscores only)";
            if (FieldNamed(container, parameter.Name) is null)
                return $"parameter '{parameter.Name}' is not a field of '{container.Name}'";
            if (parameter.Source is not ConstantValueSource && parameter.Source is not CurrentTimestampValueSource)
                return $"the value source for parameter '{parameter.Name}' is a constant or the current timestamp";
        }

        return null;
    }

    private static IField? FieldNamed(IStorageContainer container, string name) =>
        container.Schema.Fields.FirstOrDefault(f => string.Equals(f.Name.TrimStart('@'), name, StringComparison.OrdinalIgnoreCase));

    private static (SqlCommand Command, string? Problem) Statement(ExecuteOptions options, IStorageContainer container, IDatabasePath path)
    {
        var command = CreateCommand(string.Empty);
        var context = new MsSqlValueSourceContext(command, path.Dialect, "src");
        var declarations = new List<string>();
        var arguments = new List<string>();

        for (var index = 0; index < options.Parameters.Count; index++)
        {
            var parameter = options.Parameters[index];
            var expression = MsSqlValueSourceRenderers.ByName(parameter.Source.Kind).Render(parameter.Source, context);
            if (expression is null)
                return (command, $"the value source for parameter '{parameter.Name}' cannot be rendered");

            var value = expression;
            if (parameter.Source is CurrentTimestampValueSource)
            {
                value = $"@fdw_arg{index}";
                declarations.Add($"DECLARE {value} datetimeoffset(7) = {expression}; ");
            }

            arguments.Add($"@{FieldNamed(container, parameter.Name)!.Name.TrimStart('@')} = {value}");
        }

        var call = $"EXEC {BuildQualifiedTableName(path)}";
        command.CommandText = string.Concat(declarations) + (arguments.Count == 0 ? call : $"{call} {string.Join(", ", arguments)}");
        return (command, null);
    }

    private static IGenericResult<SqlCommand> Refused(string containerName, string reason)
    {
        MsSqlExecuteTranslatorLog.InvalidOptions(NullLogger<MsSqlExecuteTranslator>.Instance, containerName, reason);
        return Failed("InvalidExecuteOptions", "Reason", reason);
    }

    private static IGenericResult<SqlCommand> Failed(string code, string detailName, string detail) =>
        GenericResult<SqlCommand>.Failure(MsSqlDataResultCodes.ByName(code), ResultDetails.Create(detailName, detail));
}
