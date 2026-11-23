using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Data.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Schema.Ddl;
using Fdw.Schema.Ddl.Commands;
using Fdw.Schema.Ddl.Definitions;
using Fdw.Services.Connections;
using Fdw.Services.Data;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Services.Connections.Clients.Models;
using Fdw.Web.RestEndpoints.Logging;
using Fdw.Web.RestEndpoints.ErrorMapping;

namespace Fdw.Schema.Endpoints.Ddl;

/// <summary>
/// Abstract base endpoint for generating DDL from a connection's discovered schema.
/// Implementers must override <see cref="DiscoverContainers"/> and <see cref="GetConnectionType"/>
/// to provide connection-specific logic.
/// </summary>
/// <remarks>
/// This base class handles schema-to-DDL conversion, error handling, and HTTP response
/// formatting. Subclasses implement connection resolution and schema discovery.
/// </remarks>
public abstract class GenerateDdlEndpointBase : Endpoint<GenerateDdlRequest, GenerateDdlResponse>
{
    private readonly ILogger<GenerateDdlEndpointBase> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GenerateDdlEndpointBase"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    protected GenerateDdlEndpointBase(ILogger<GenerateDdlEndpointBase>? logger = null)
    {
        _logger = logger ?? NullLogger<GenerateDdlEndpointBase>.Instance;
    }

    /// <summary>
    /// Gets the route for this endpoint. Default is "/connections/{Name}/generate-ddl".
    /// </summary>
    protected virtual string Route => "/connections/{Name}/generate-ddl";

    /// <summary>
    /// Gets the policy name for authorization. Default is "schema:write".
    /// </summary>
    protected virtual string PolicyName => "schema:write";

    /// <summary>Configures the endpoint route, policies, and OpenAPI metadata.</summary>
    public override void Configure()
    {
        Post(Route);
#if DEVELOP
        AllowAnonymous();
#else
        Policies(PolicyName);
#endif
        Summary(s =>
        {
            s.Summary = "Generate DDL from schema";
            s.Description = "Discovers the schema of a connection and generates DDL (CREATE TABLE) statements.";
        });

        OnBeforeConfiguring();
    }

    /// <summary>
    /// Virtual hook called at the end of <see cref="Configure"/>.
    /// Override to add additional endpoint configuration.
    /// </summary>
    protected virtual void OnBeforeConfiguring()
    {
    }

    /// <summary>
    /// Handles the request by discovering schema and generating DDL statements.
    /// </summary>
    public override async Task HandleAsync(GenerateDdlRequest req, CancellationToken ct)
    {
        SchemaEndpointLog.DiscoveringSchema(_logger, req.Name);

        try
        {
            var connectionType = await GetConnectionType(req.Name, ct).ConfigureAwait(false);
            if (string.IsNullOrEmpty(connectionType))
            {
                await Send.ResultAsync(ResultHttpStatusMapper.Answer(GenericResult.Failure(SchemaEndpointLog.GenerateDdlUnableToDetermineConnectionType(_logger)), HttpContext)).ConfigureAwait(false);
                return;
            }

            var generator = DdlGeneratorFor(connectionType);
            if (generator.IsFailure)
            {
                await Send.ResultAsync(ResultHttpStatusMapper.Answer(generator, HttpContext)).ConfigureAwait(false);
                return;
            }

            // The discovery's choices come from the request (FDW-3823); one left out is refused, naming it.
            var discovery = DataStoreDiscoveryChoices.FromRequest(
                req.ExcludedSchemas, req.IncludeOnlySchemas, req.DiscoverViews, req.DiscoverIndexes, req.DiscoverForeignKeys,
                req.DiscoverDescriptions, req.Name, _logger);
            if (discovery.IsFailure)
            {
                await Send.ResultAsync(ResultHttpStatusMapper.Answer(discovery, HttpContext)).ConfigureAwait(false);
                return;
            }

            await AnswerWithDdl(req, generator.Value!, discovery.Value!, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            SchemaEndpointLog.SchemaOperationFailed(_logger, ex, "generate-ddl", req.Name);
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(GenericResult.Failure(SchemaEndpointLog.GenerateDdlFaulted(_logger)), HttpContext)).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Discovers the containers with the stated choices and answers the DDL the connection type's generator writes for them.
    /// </summary>
    private async Task AnswerWithDdl(GenerateDdlRequest req, IDdlGenerator generator, DataStoreDiscoveryOptions options, CancellationToken ct)
    {
        var containersResult = await DiscoverContainers(req.Name, options, ct).ConfigureAwait(false);

        if (containersResult.IsFailure)
        {
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(containersResult, HttpContext)).ConfigureAwait(false);
            return;
        }

        if (containersResult.Value == null)
        {
            SchemaEndpointLog.SchemaConnectionNotFound(_logger, req.Name, "generate-ddl");
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(GenericResult.Failure(SchemaEndpointLog.GenerateDdlConnectionNotFound(_logger, req.Name)), HttpContext)).ConfigureAwait(false);
            return;
        }

        var commands = BuildCreateTableCommands(containersResult.Value, req.SchemaFilter);
        var warnings = WarningsOf(containersResult, containersResult.Value, req.SchemaFilter);
        var script = generator.GenerateScript(commands);
        if (script.IsFailure)
        {
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(script, HttpContext)).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(new GenerateDdlResponse
        {
            ConnectionName = req.Name,
            Ddl = script.Value!,
            StatementCount = commands.Count,
            Warnings = warnings,
        }, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Discovers storage containers for the specified connection.
    /// </summary>
    /// <param name="connectionName">The connection name.</param>
    /// <param name="options">The discovery choices the request stated.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the discovered storage containers.</returns>
    protected abstract Task<IGenericResult<IReadOnlyList<IStorageContainer>>> DiscoverContainers(
        string connectionName,
        DataStoreDiscoveryOptions options,
        CancellationToken ct);

    /// <summary>
    /// Gets the connection type string for the specified connection (e.g., "MsSql", "PostgreSql").
    /// </summary>
    /// <param name="connectionName">The connection name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The connection type string, or null if not found.</returns>
    protected abstract Task<string?> GetConnectionType(string connectionName, CancellationToken ct);

    /// <summary>
    /// Gets the DDL generator of the connection's type: the dialect of the engine the connection is on.
    /// </summary>
    /// <remarks>
    /// Why the connection type decides: the endpoint names no engine, and a type that cannot say its dialect is refused with the
    /// coded <c>DdlDialectNotOffered</c> (FDW-5959), never given another engine's text. A type name that is not a registered
    /// connection type is refused too.
    /// </remarks>
    /// <param name="connectionType">The connection type's name, as <see cref="GetConnectionType"/> answered it.</param>
    /// <returns>The generator, or the refusal.</returns>
    protected virtual IGenericResult<IDdlGenerator> DdlGeneratorFor(string connectionType)
    {
        var type = ConnectionTypes.ByName(connectionType);

        // Why ReferenceEquals: ByName answers an unknown name with the NotFound sentinel, never null.
        return ReferenceEquals(type, ConnectionTypes.NotFound)
            ? GenericResult<IDdlGenerator>.Failure(SchemaEndpointLog.GenerateDdlConnectionTypeUnknown(_logger, connectionType))
            : type.DdlGenerator();
    }

    /// <summary>
    /// Builds a CREATE TABLE command for each discovered table, in the order the containers were discovered, grouped by schema.
    /// The engine's text is the generator's: nothing here writes SQL.
    /// </summary>
    /// <param name="containers">The discovered storage containers.</param>
    /// <param name="schemaFilter">Optional schema name filter.</param>
    /// <returns>The commands.</returns>
    protected virtual IReadOnlyList<IDdlCommand> BuildCreateTableCommands(
        IReadOnlyList<IStorageContainer> containers,
        string? schemaFilter)
        => [.. TablesToGenerate(containers, schemaFilter)
            .SelectMany(group => group.Select(container => CreateTable(group.Key, container)))];

    /// <summary>
    /// Lists what the generated DDL does not carry of the schema, and the warnings the discovery returned with its success: one for each
    /// foreign key of a table the DDL is generated for, because a foreign key in the DDL is one column with its referential actions, and
    /// the discovery holds neither (FDW-6309).
    /// </summary>
    /// <param name="discovery">The discovery's result, whose warnings are carried over.</param>
    /// <param name="containers">The discovered storage containers.</param>
    /// <param name="schemaFilter">Optional schema name filter.</param>
    /// <returns>The warnings, in the order of the containers.</returns>
    protected virtual IList<GenerateDdlWarning> WarningsOf(IGenericResult discovery, IReadOnlyList<IStorageContainer> containers, string? schemaFilter)
    {
        var warnings = new List<IGenericMessage>(discovery.Messages);
        foreach (var group in TablesToGenerate(containers, schemaFilter))
        {
            foreach (var container in group)
            {
                foreach (var key in container.Keys.Where(key => key.KeyType.IsReference))
                    warnings.Add(SchemaEndpointLog.GenerateDdlForeignKeyNotGenerated(_logger, key.KeyName, group.Key, container.Name));
            }
        }

        return [.. warnings.Select(message => new GenerateDdlWarning { Code = message.Code ?? string.Empty, Message = message.Message })];
    }

    private static IEnumerable<IGrouping<string, IStorageContainer>> TablesToGenerate(IReadOnlyList<IStorageContainer> containers, string? schemaFilter)
        => containers
            .Where(c => c.SupportedOperations.Contains("Insert", StringComparer.Ordinal))
            .GroupBy(c => c.Path.PathValue, StringComparer.OrdinalIgnoreCase)
            .Where(group => string.IsNullOrEmpty(schemaFilter) || string.Equals(group.Key, schemaFilter, StringComparison.OrdinalIgnoreCase));

    private static CreateTableCommand CreateTable(string schemaName, IStorageContainer container)
    {
        // The primary key the container declares, the whole key. It was the identity columns, which are not the key: a table whose key
        // is not an identity column was generated with none, and a table with an identity column that is not its key with a wrong one.
        var keyColumns = container.PrimaryKeyFieldNames();

        return new CreateTableCommand
        {
            SchemaName = schemaName,
            TableName = container.Name,
            Columns = [.. container.Schema.Fields.Select(field => new DdlColumnDefinition
            {
                Name = field.Name,
                SqlType = field.FieldType.TypeName,
                IsNullable = field.IsNullable,
                IsIdentity = field.IsIdentity,
            })],
            PrimaryKeyName = keyColumns.Count > 0 ? $"PK_{container.Name}" : null,
            PrimaryKeyColumns = keyColumns.Count > 0 ? keyColumns : null,
        };
    }
}
