using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Results.Abstractions;
using Fdw.Services.Connections.Http.Abstractions;
using Fdw.Services.Connections.Http.Abstractions.OptionTypes;
using Fdw.Services.Connections.Http.Abstractions.OptionTypes.HttpProtocolOptions;
using Fdw.Services.Connections.Http.Abstractions.Results;

namespace Fdw.Services.Connections.Http.Protocols;

/// <summary>
/// Base class for GraphQL protocol implementations.
/// </summary>
/// <remarks>
/// <para>
/// This base class handles common GraphQL concerns:
/// <list type="bullet">
/// <item><description>Query/mutation building from data commands</description></item>
/// <item><description>Variables serialization</description></item>
/// <item><description>Response parsing (data + errors)</description></item>
/// <item><description>Error handling and extraction</description></item>
/// </list>
/// </para>
/// <para>
/// Derived classes override virtual methods to customize:
/// <list type="bullet">
/// <item><description><see cref="BuildGraphQLQuery"/> - Build query string from command</description></item>
/// <item><description><see cref="BuildVariables"/> - Build variables object from command</description></item>
/// <item><description><see cref="GetOperationName"/> - Determine operation name</description></item>
/// <item><description><see cref="ExtractDataFromResponse"/> - Extract typed data from response</description></item>
/// </list>
/// </para>
/// <para>
/// GraphQL request format:
/// <code>
/// {
///   "query": "query GetUsers($filter: UserFilter) { users(filter: $filter) { id name } }",
///   "operationName": "GetUsers",
///   "variables": { "filter": { "active": true } }
/// }
/// </code>
/// </para>
/// </remarks>
public abstract class GraphQLProtocolBase : HttpProtocolBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GraphQLProtocolBase"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for the protocol.</param>
    /// <param name="name">The name of the protocol.</param>
    /// <param name="description">The description of the protocol.</param>
    protected GraphQLProtocolBase(int id, string name, string description)
        : base(id, name, description, "application/json")
    {
    }

    /// <inheritdoc/>
    [ExcludeFromCodeCoverage(Justification = "Orchestrating method - tested via integration tests")]
    public override async Task<IGenericResult<HttpRequestMessage>> Translate(
        IDataCommand command,
        IStorageContainer container,
        HttpProtocolContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            // GraphQL always uses POST
            var endpoint = GetRequestPath(command, container, context);
            var request = new HttpRequestMessage(HttpMethod.Post, endpoint);

            // Build GraphQL request body
            var bodyResult = await BuildGraphQLRequestBody(command, container, context, cancellationToken).ConfigureAwait(false);
            if (!bodyResult.IsSuccess)
            {
                return bodyResult.ToNewResult<HttpRequestMessage>();
            }

            request.Content = new StringContent(bodyResult.Value!, Encoding.UTF8, DefaultContentType);

            // Configure headers
            ConfigureRequestHeaders(request, command, container, context);
            ConfigureGraphQLHeaders(request, context);

            return GenericResult<HttpRequestMessage>.Success(request);
        }
        catch (Exception ex)
        {
            return GenericResult<HttpRequestMessage>.Failure(
                HttpResultCodes.ByName("GraphQLRequestBuildFailed"),
                ResultDetails.Create().With("ErrorMessage", ex.Message));
        }
    }

    /// <inheritdoc/>
    [ExcludeFromCodeCoverage(Justification = "Orchestrating method - tested via integration tests")]
    public override async Task<IGenericResult<object?>> ProcessResponse(
        HttpResponseMessage response,
        IStorageContainer container,
        Type resultType,
        HttpProtocolContext context,
        CancellationToken cancellationToken)
    {
        var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        // GraphQL may return 200 even with errors
        if (string.IsNullOrWhiteSpace(content))
        {
            if (!response.IsSuccessStatusCode)
            {
                return GenericResult<object?>.Failure(
                    HttpResultCodes.ByName("GraphQLHttpError"),
                    ResultDetails.Create()
                        .With("StatusCode", (int)response.StatusCode)
                        .With("ReasonPhrase", response.ReasonPhrase ?? "Unknown"));
            }
            return GenericResult<object?>.Success(null);
        }

        GraphQLResponse? graphqlResponse;
        try
        {
            graphqlResponse = JsonSerializer.Deserialize<GraphQLResponse>(content, DefaultJsonOptions);
        }
        catch (Exception ex)
        {
            return GenericResult<object?>.Failure(
                HttpResultCodes.ByName("GraphQLResponseParseFailed"),
                ResultDetails.Create().With("ErrorMessage", ex.Message));
        }

        if (graphqlResponse is null)
        {
            return GenericResult<object?>.Failure(HttpResultCodes.ByName("GraphQLEmptyResponse"));
        }

        // Check for GraphQL errors
        if (graphqlResponse.Errors is not null && graphqlResponse.Errors.Count > 0)
        {
            var errorMessages = new List<string>();
            foreach (var error in graphqlResponse.Errors)
            {
                var message = FormatGraphQLError(error);
                errorMessages.Add(message);
            }
            return GenericResult<object?>.Failure(
                HttpResultCodes.ByName("GraphQLError"),
                ResultDetails.Create().With("ErrorMessage", string.Join("; ", errorMessages)));
        }

        // HTTP error without GraphQL errors
        if (!response.IsSuccessStatusCode)
        {
            return GenericResult<object?>.Failure(
                HttpResultCodes.ByName("GraphQLHttpError"),
                ResultDetails.Create()
                    .With("StatusCode", (int)response.StatusCode)
                    .With("ReasonPhrase", response.ReasonPhrase ?? "Unknown"));
        }

        // Extract data
        return await ExtractDataFromResponse(graphqlResponse, resultType, context, cancellationToken).ConfigureAwait(false);
    }

    #region Virtual Extension Points

    /// <summary>
    /// Builds the complete GraphQL request body.
    /// </summary>
    /// <param name="command">The data command.</param>
    /// <param name="container">The storage container.</param>
    /// <param name="context">The protocol context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The JSON request body.</returns>
    protected virtual async Task<IGenericResult<string>> BuildGraphQLRequestBody(
        IDataCommand command,
        IStorageContainer container,
        HttpProtocolContext context,
        CancellationToken cancellationToken)
    {
        var queryResult = await BuildGraphQLQuery(command, container, context, cancellationToken).ConfigureAwait(false);
        if (!queryResult.IsSuccess)
        {
            return queryResult.ToNewResult<string>();
        }

        var variables = await BuildVariables(command, container, context, cancellationToken).ConfigureAwait(false);
        var operationName = GetOperationName(command, container, context);

        var requestBody = new GraphQLRequest
        {
            Query = queryResult.Value!,
            OperationName = operationName,
            Variables = variables
        };

        var json = JsonSerializer.Serialize(requestBody, DefaultJsonOptions);
        return GenericResult<string>.Success(json);
    }

    /// <summary>
    /// Builds the GraphQL query or mutation string from the data command.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Override this method to customize query generation. By default, uses the command's
    /// Metadata["GraphQLQuery"] if available, otherwise builds a simple query from the command.
    /// </para>
    /// <para>
    /// For complex scenarios, you might generate queries from:
    /// <list type="bullet">
    /// <item><description>Pre-defined query templates</description></item>
    /// <item><description>Introspection-based query building</description></item>
    /// <item><description>Command metadata containing full queries</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <param name="command">The data command.</param>
    /// <param name="container">The storage container with schema info.</param>
    /// <param name="context">The protocol context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The GraphQL query string.</returns>
    protected virtual Task<IGenericResult<string>> BuildGraphQLQuery(
        IDataCommand command,
        IStorageContainer container,
        HttpProtocolContext context,
        CancellationToken cancellationToken)
    {
        // Check for explicit query in metadata
        if (command.Metadata.TryGetValue("GraphQLQuery", out var queryObj) && queryObj is string explicitQuery)
        {
            return Task.FromResult(GenericResult<string>.Success(explicitQuery));
        }

        // Build query from command type
        var query = command.CommandType switch
        {
            "Query" or "Select" => BuildQueryOperation(command, container),
            "Insert" or "Create" => BuildMutationOperation(command, container, "create"),
            "Update" => BuildMutationOperation(command, container, "update"),
            "Delete" => BuildMutationOperation(command, container, "delete"),
            _ => BuildQueryOperation(command, container)
        };

        return Task.FromResult(GenericResult<string>.Success(query));
    }

    /// <summary>
    /// Builds a query operation from the command.
    /// </summary>
    protected virtual string BuildQueryOperation(IDataCommand command, IStorageContainer container)
    {
        var typeName = GetGraphQLTypeName(container.Name);
        var fieldList = BuildFieldSelection(container);

        // Build filter arguments if present
        var args = BuildQueryArguments(command);

        return $"query {{ {typeName}{args} {{ {fieldList} }} }}";
    }

    /// <summary>
    /// Builds a mutation operation from the command.
    /// </summary>
    protected virtual string BuildMutationOperation(IDataCommand command, IStorageContainer container, string action)
    {
        var typeName = GetGraphQLTypeName(container.Name);
        var mutationName = $"{action}{typeName}";
        var fieldList = BuildFieldSelection(container);

        return $"mutation {{ {mutationName}(input: $input) {{ {fieldList} }} }}";
    }

    /// <summary>
    /// Gets the GraphQL type name from the container name.
    /// </summary>
    protected virtual string GetGraphQLTypeName(string containerName)
    {
        // Convert to camelCase for GraphQL convention
        if (string.IsNullOrEmpty(containerName))
        {
            return "data";
        }

        return char.ToLowerInvariant(containerName[0]) + containerName.Substring(1);
    }

    /// <summary>
    /// Builds the field selection for the query.
    /// </summary>
    protected virtual string BuildFieldSelection(IStorageContainer container)
    {
        // Use container schema if available
        if (container?.Schema?.Fields is not null && container.Schema.Fields.Count > 0)
        {
            var fieldNames = new List<string>();
            foreach (var field in container.Schema.Fields)
            {
                fieldNames.Add(field.Name);
            }
            return string.Join(" ", fieldNames);
        }

        // Default: request id and common fields
        return "id";
    }

    /// <summary>
    /// Builds query arguments from the command (filters, pagination, etc.).
    /// </summary>
    protected virtual string BuildQueryArguments(IDataCommand command)
    {
        var args = new List<string>();

        if (command is IQueryCommand queryCommand)
        {
            // Add pagination
            if (queryCommand.Paging is not null)
            {
                if (queryCommand.Paging.Skip > 0)
                {
                    args.Add($"skip: {queryCommand.Paging.Skip}");
                }
                if (queryCommand.Paging.Take > 0)
                {
                    args.Add($"take: {queryCommand.Paging.Take}");
                }
            }

            // Add filter reference (actual filter goes in variables)
            if (queryCommand.Filter is not null)
            {
                args.Add("filter: $filter");
            }

            // Add ordering
            if (queryCommand.Ordering?.OrderedFields?.Count > 0)
            {
                args.Add("orderBy: $orderBy");
            }
        }

        return args.Count > 0 ? $"({string.Join(", ", args)})" : string.Empty;
    }

    /// <summary>
    /// Builds the variables object for the GraphQL request.
    /// </summary>
    /// <param name="command">The data command.</param>
    /// <param name="container">The storage container.</param>
    /// <param name="context">The protocol context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The variables dictionary, or null if no variables.</returns>
    protected virtual Task<IDictionary<string, object?>?> BuildVariables(
        IDataCommand command,
        IStorageContainer container,
        HttpProtocolContext context,
        CancellationToken cancellationToken)
    {
        var variables = new Dictionary<string, object?>(StringComparer.Ordinal);

        // Add input data for mutations
        if (command is IDataCommandWithInput commandWithInput && commandWithInput.InputData is not null)
        {
            variables["input"] = commandWithInput.InputData;
        }

        // Add filter variables
        if (command is IQueryCommand queryCommand && queryCommand.Filter is not null)
        {
            variables["filter"] = BuildFilterVariable(queryCommand.Filter);
        }

        // Add ordering variables
        if (command is IQueryCommand qc && qc.Ordering?.OrderedFields?.Count > 0)
        {
            var orderBy = new List<IDictionary<string, string>>();
            foreach (var field in qc.Ordering.OrderedFields)
            {
                orderBy.Add(new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["field"] = field.PropertyName,
                    ["direction"] = field.Direction.Name.ToUpperInvariant()
                });
            }
            variables["orderBy"] = orderBy;
        }

        return Task.FromResult<IDictionary<string, object?>?>(variables.Count > 0 ? variables : null);
    }

    /// <summary>
    /// Builds the filter variable from a filter expression.
    /// </summary>
    protected virtual object? BuildFilterVariable(IFilterExpression filter)
    {
        if (filter.Root is null)
        {
            return null;
        }

        return BuildFilterObjectFromExpression(filter.Root);
    }

    /// <summary>
    /// Converts a filter node to a GraphQL filter object.
    /// </summary>
    protected virtual object? BuildFilterObjectFromExpression(IFilterNode node)
    {
        if (node is IFilterCondition condition)
        {
            var filterObj = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                [condition.PropertyName] = new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    [MapOperatorToGraphQL(condition.Operator?.Name ?? "Equal")] = condition.Value
                }
            };
            return filterObj;
        }

        if (node is FilterGroup group)
        {
            var conditions = new List<object?>();
            foreach (var child in group.Nodes)
            {
                var childFilter = BuildFilterObjectFromExpression(child);
                if (childFilter is not null)
                {
                    conditions.Add(childFilter);
                }
            }

            var logicalOp = group.Operator.Name;
            return new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                [logicalOp.ToUpperInvariant()] = conditions
            };
        }

        return null;
    }

    /// <summary>
    /// Maps a filter operator to GraphQL convention.
    /// </summary>
    protected virtual string MapOperatorToGraphQL(string operatorName)
    {
        return operatorName switch
        {
            "Equal" => "eq",
            "NotEqual" => "ne",
            "GreaterThan" => "gt",
            "GreaterThanOrEqual" => "gte",
            "LessThan" => "lt",
            "LessThanOrEqual" => "lte",
            "Contains" => "contains",
            "StartsWith" => "startsWith",
            "EndsWith" => "endsWith",
            "In" => "in",
            "NotIn" => "notIn",
            _ => "eq"
        };
    }

    /// <summary>
    /// Gets the operation name for the GraphQL request.
    /// </summary>
    /// <param name="command">The data command.</param>
    /// <param name="container">The storage container.</param>
    /// <param name="context">The protocol context.</param>
    /// <returns>The operation name, or null for anonymous operations.</returns>
    protected virtual string? GetOperationName(
        IDataCommand command,
        IStorageContainer container,
        HttpProtocolContext context)
    {
        // Check for explicit operation name in metadata
        if (command.Metadata.TryGetValue("GraphQLOperationName", out var nameObj) && nameObj is string name)
        {
            return name;
        }

        // Why: Addressing comes from container, not command. Container.Name is always set.
        var typeName = container.Name;
        return command.CommandType switch
        {
            "Query" or "Select" => $"Get{typeName}",
            "Insert" or "Create" => $"Create{typeName}",
            "Update" => $"Update{typeName}",
            "Delete" => $"Delete{typeName}",
            _ => null
        };
    }

    /// <summary>
    /// Configures GraphQL-specific headers.
    /// </summary>
    /// <param name="request">The HTTP request.</param>
    /// <param name="context">The protocol context.</param>
    protected virtual void ConfigureGraphQLHeaders(HttpRequestMessage request, HttpProtocolContext context)
    {
        // Add Accept header for GraphQL
        request.Headers.TryAddWithoutValidation("Accept", "application/json");

        // Add API key if configured
        if (!string.IsNullOrEmpty(context.ResolvedApiKey))
        {
            string? headerName = null;
            if (context.Configuration is HttpConnectionConfigurationBase httpConfig
                && string.Equals(httpConfig.AuthenticationType, "ApiKey", StringComparison.OrdinalIgnoreCase)
                && httpConfig.AdditionalProperties is { Count: > 0 } securityValues)
            {
                securityValues.TryGetValue("ApiKeyHeaderName", out headerName);
            }

            request.Headers.TryAddWithoutValidation(headerName ?? "X-API-Key", context.ResolvedApiKey);
        }
    }

    /// <summary>
    /// Extracts the typed data from the GraphQL response.
    /// </summary>
    /// <param name="response">The parsed GraphQL response.</param>
    /// <param name="resultType">The expected result type.</param>
    /// <param name="context">The protocol context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The extracted result.</returns>
    [ExcludeFromCodeCoverage(Justification = "Exception path requires type mismatch during deserialization")]
    protected virtual Task<IGenericResult<object?>> ExtractDataFromResponse(
        GraphQLResponse response,
        Type resultType,
        HttpProtocolContext context,
        CancellationToken cancellationToken)
    {
        if (response.Data is null)
        {
            return Task.FromResult(GenericResult<object?>.Success(null));
        }

        try
        {
            // GraphQL data is a JsonElement, need to deserialize to target type
            var dataJson = response.Data.Value.GetRawText();

            // Handle string result type
            if (resultType == typeof(string))
            {
                return Task.FromResult(GenericResult<object?>.Success(dataJson));
            }

            // The data is typically { "fieldName": actualData }
            // Try to extract the first property value
            if (response.Data.Value.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in response.Data.Value.EnumerateObject())
                {
                    var propertyJson = property.Value.GetRawText();
                    var result = JsonSerializer.Deserialize(propertyJson, resultType, DefaultJsonOptions);
                    return Task.FromResult(GenericResult<object?>.Success(result));
                }
            }

            // Deserialize directly
            var directResult = JsonSerializer.Deserialize(dataJson, resultType, DefaultJsonOptions);
            return Task.FromResult(GenericResult<object?>.Success(directResult));
        }
        catch (Exception ex)
        {
            return Task.FromResult(GenericResult<object?>.Failure(
                HttpResultCodes.ByName("GraphQLDeserializationFailed"),
                ResultDetails.Create().With("ErrorMessage", ex.Message)));
        }
    }

    /// <summary>
    /// Formats a GraphQL error for display.
    /// </summary>
    /// <param name="error">The GraphQL error.</param>
    /// <returns>A formatted error message.</returns>
    protected virtual string FormatGraphQLError(GraphQLError error)
    {
        var message = error.Message ?? "Unknown error";

        if (error.Locations is not null && error.Locations.Count > 0)
        {
            var loc = error.Locations[0];
            message += $" at line {loc.Line}, column {loc.Column}";
        }

        if (error.Path is not null && error.Path.Count > 0)
        {
            message += $" (path: {string.Join(".", error.Path)})";
        }

        return message;
    }

    #endregion
}

#region GraphQL Types

#endregion
