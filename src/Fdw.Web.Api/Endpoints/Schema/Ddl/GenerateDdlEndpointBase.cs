using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Services.Connections.Clients.Models;
using Fdw.Web.RestEndpoints.Logging;

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
                AddError("Unable to determine connection type");
                await Send.ErrorsAsync(400, ct).ConfigureAwait(false);
                return;
            }

            var containersResult = await DiscoverContainers(req.Name, ct).ConfigureAwait(false);

            if (!containersResult.IsSuccess || containersResult.Value == null)
            {
                var message = containersResult.CurrentMessage ?? "Schema discovery failed";
                SchemaEndpointLog.SchemaConnectionNotFound(_logger, req.Name, "generate-ddl");
                AddError(message);
                await Send.ErrorsAsync(containersResult.IsSuccess ? 404 : 500, ct).ConfigureAwait(false);
                return;
            }

            var containers = containersResult.Value;
            var ddl = GenerateCreateTableStatements(containers, connectionType, req.SchemaFilter);
            var statementCount = ddl.Split("CREATE TABLE", StringSplitOptions.None).Length - 1;

            await Send.OkAsync(new GenerateDdlResponse
            {
                ConnectionName = req.Name,
                Ddl = ddl,
                StatementCount = statementCount
            }, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            SchemaEndpointLog.SchemaOperationFailed(_logger, ex, "generate-ddl", req.Name);
            AddError("DDL generation failed");
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Discovers storage containers for the specified connection.
    /// </summary>
    /// <param name="connectionName">The connection name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the discovered storage containers.</returns>
    protected abstract Task<IGenericResult<IReadOnlyList<IStorageContainer>>> DiscoverContainers(
        string connectionName,
        CancellationToken ct);

    /// <summary>
    /// Gets the connection type string for the specified connection (e.g., "MsSql", "PostgreSql").
    /// </summary>
    /// <param name="connectionName">The connection name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The connection type string, or null if not found.</returns>
    protected abstract Task<string?> GetConnectionType(string connectionName, CancellationToken ct);

    /// <summary>
    /// Generates CREATE TABLE DDL statements from discovered containers.
    /// Override to customize DDL generation for specific database engines.
    /// </summary>
    /// <param name="containers">The discovered storage containers.</param>
    /// <param name="connectionType">The connection type (e.g., "MsSql", "PostgreSql").</param>
    /// <param name="schemaFilter">Optional schema name filter.</param>
    /// <returns>The generated DDL script.</returns>
    protected virtual string GenerateCreateTableStatements(
        IReadOnlyList<IStorageContainer> containers,
        string connectionType,
        string? schemaFilter)
    {
        var sb = new StringBuilder();
        var quoteOpen = string.Equals(connectionType, "PostgreSql", StringComparison.OrdinalIgnoreCase) ? "\"" : "[";
        var quoteClose = string.Equals(connectionType, "PostgreSql", StringComparison.OrdinalIgnoreCase) ? "\"" : "]";

        var groups = containers
            .Where(c => !string.Equals(c.ContainerType.Name, "View", StringComparison.Ordinal))
            .GroupBy(c => c.Path.PathValue, StringComparer.OrdinalIgnoreCase);

        foreach (var group in groups)
        {
            if (!string.IsNullOrEmpty(schemaFilter) &&
                !string.Equals(group.Key, schemaFilter, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            sb.AppendLine(CultureInfo.InvariantCulture, $"-- Schema: {group.Key}");

            foreach (var container in group)
            {
                sb.AppendLine(CultureInfo.InvariantCulture, $"CREATE TABLE {quoteOpen}{group.Key}{quoteClose}.{quoteOpen}{container.Name}{quoteClose} (");

                var fields = container.Schema.Fields;
                for (var i = 0; i < fields.Count; i++)
                {
                    var field = fields[i];
                    var nullable = field.IsNullable ? "NULL" : "NOT NULL";
                    var identity = field.IsIdentity ? " IDENTITY(1,1)" : string.Empty;
                    var separator = i < fields.Count - 1 ? "," : string.Empty;

                    sb.AppendLine(CultureInfo.InvariantCulture, $"    {quoteOpen}{field.Name}{quoteClose} {field.FieldType.TypeName}{identity} {nullable}{separator}");
                }

                var pkFields = container.Schema.GetIdentityFields().ToList();
                if (pkFields.Count > 0)
                {
                    var pkCols = string.Join(", ", pkFields.Select(f => $"{quoteOpen}{f.Name}{quoteClose}"));
                    sb.AppendLine(CultureInfo.InvariantCulture, $"    ,CONSTRAINT {quoteOpen}PK_{container.Name}{quoteClose} PRIMARY KEY ({pkCols})");
                }

                sb.AppendLine(");");
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }
}
