using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Web.RestEndpoints.ApiReference;
using Fdw.Web.RestEndpoints.ApiReference.Logging;
using Fdw.Web.RestEndpoints.ApiReference.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>
/// Base for the API references of workspace data: one operation per data set the caller may query in each
/// workspace the reference covers, grouped by workspace. A variation the workspace owns is one of them.
/// </summary>
/// <typeparam name="TRequest">What the route carries.</typeparam>
/// <remarks>
/// <para>
/// The data route is one operation, <c>POST /{WorkspaceName}/{DataSetName}</c>. A reader cannot
/// tell from it which names are theirs to use, so the reference replaces it with one operation per
/// data set the caller may query - <c>POST /Sales/Orders</c> - each described with that data
/// set's fields, and nothing else. The route's own description (body, responses, security) is the
/// template every copy starts from, so the reference cannot drift from what the route accepts.
/// </para>
/// <para>
/// The names come from <see cref="IWorkspaceReadService.DataSets"/>, the same answer the data route resolves a name
/// from, so the reference cannot list a name the route would refuse nor omit one it would serve (FDW-1318). A data
/// set that fails to load is left out and logged there, and the others are still described: the data route serves
/// the rest of a workspace when one data set is broken, and so does its reference.
/// </para>
/// </remarks>
public abstract class WorkspaceDocumentEndpointBase<TRequest> : ApiReferenceDocumentEndpointBase<TRequest>
    where TRequest : notnull
{
    private const string DataRouteSuffix = "/{WorkspaceName}/{DataSetName}";

    private readonly IWorkspaceReadService _readService;

    /// <summary>Initializes a new instance of the <see cref="WorkspaceDocumentEndpointBase{TRequest}"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="systemRoles">Decides whether the caller is a platform admin.</param>
    /// <param name="workspaces">Resolves the workspaces with their members and resources.</param>
    /// <param name="access">Decides whether the caller may read a workspace.</param>
    /// <param name="readService">Resolves the data sets and variations a workspace exposes, with their fields.</param>
    protected WorkspaceDocumentEndpointBase(
        ILogger<ApiReferenceDocumentEndpointBase<TRequest>> logger,
        ISystemRoleConfiguration systemRoles,
        IWorkspaceConfigurationProvider workspaces,
        IWorkspaceAccessPolicy access,
        IWorkspaceReadService readService)
        : base(logger, systemRoles)
    {
        Workspaces = workspaces ?? throw new ArgumentNullException(nameof(workspaces));
        Access = access ?? throw new ArgumentNullException(nameof(access));
        _readService = readService ?? throw new ArgumentNullException(nameof(readService));
    }

    /// <summary>Gets the provider that resolves workspaces.</summary>
    protected IWorkspaceConfigurationProvider Workspaces { get; }

    /// <summary>Gets the policy that decides whether the caller may read a workspace.</summary>
    protected IWorkspaceAccessPolicy Access { get; }

    /// <summary>Replaces the whole document with the data set operations of the given workspaces.</summary>
    /// <param name="document">This request's copy of the document.</param>
    /// <param name="workspaces">The workspaces to describe; the caller must be allowed to read each.</param>
    /// <param name="title">The reference's title.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Success, or a failure when the document has no data route to copy.</returns>
    protected async Task<IGenericResult> Describe(
        ApiReferenceDocument document,
        IReadOnlyList<IWorkspaceImplementationConfiguration> workspaces,
        string title,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(workspaces);

        if (document.Paths.FirstOrDefault(path => path.EndsWith(DataRouteSuffix, StringComparison.OrdinalIgnoreCase)) is not { } routePath)
        {
            return GenericResult.Failure(
                ApiReferenceResultCodes.ByName("DocumentUnreadable"), Logger,
                ResultDetails.Create("reason", "it has no data route ending " + DataRouteSuffix + " to describe data sets from"));
        }

        var route = document.Operation(routePath, "post", Logger);
        if (route.IsFailure || route.Value is not { } routeOperation)
            return route;

        if (BodyContent(routeOperation) is not { Count: > 0 })
        {
            return GenericResult.Failure(
                ApiReferenceResultCodes.ByName("DocumentUnreadable"), Logger,
                ResultDetails.Create("reason", "its data route " + routePath + " declares no request body to give each data set an example of"));
        }

        // Copied before the paths go: the template outlives the route it was read from.
        var template = routeOperation.DeepClone().AsObject();
        document.KeepPaths(_ => false, Logger);
        document.SetTitle(title);
        document.DescribeBearerSchemes(
            "An API key or a bearer token. In the browser, the session you signed in with is used instead.");

        var tags = new List<KeyValuePair<string, string?>>();
        foreach (var workspace in workspaces)
        {
            tags.Add(new KeyValuePair<string, string?>(workspace.Name, workspace.Description));
            var described = await AddDataSets(document, routePath, template, workspace, cancellationToken).ConfigureAwait(false);
            if (described.IsFailure)
                return described;
        }

        document.ReplaceTags(tags);
        return GenericResult.Success();
    }

    private static string PathFor(string routePath, string workspaceName, string dataSetName)
        => routePath
            .Replace("{WorkspaceName}", Uri.EscapeDataString(workspaceName), StringComparison.OrdinalIgnoreCase)
            .Replace("{DataSetName}", Uri.EscapeDataString(dataSetName), StringComparison.OrdinalIgnoreCase);

    private static JsonObject OperationFor(JsonObject template, string workspaceName, ReadableDataSet dataSet)
    {
        var operation = template.DeepClone().AsObject();
        operation["operationId"] = "Query_" + workspaceName + "_" + dataSet.Name;
        operation["summary"] = "Query " + dataSet.Name;
        operation["description"] = DataSetReferenceDescription.For(dataSet);
        operation["tags"] = new JsonArray(JsonValue.Create(workspaceName));

        // Why an empty object: an example holds the body's required members and nothing else (FDW-1125). The generator's
        // own wrote every member, null where it had no value, and the route refused it. The query body has no required
        // member -- the workspace and data set are in the path -- so the example is {}, which reads the first page. Filter,
        // sort, skip and take stay in the schema as optional members the reader may add.
        foreach (var (_, mediaType) in BodyContent(operation)!)
        {
            if (mediaType is not JsonObject described)
                continue;

            described.Remove("examples");
            described["example"] = new JsonObject();
        }

        // The names are in the path now, so the two route parameters are not the reader's to fill in.
        if (operation["parameters"] is JsonArray parameters)
        {
            foreach (var routeParameter in parameters.Where(IsPathParameter).ToList())
                parameters.Remove(routeParameter);
        }

        return operation;
    }

    // The media types the operation's request body is described in, or null when it declares no body.
    private static JsonObject? BodyContent(JsonObject operation)
        => operation["requestBody"] is JsonObject body && body["content"] is JsonObject content ? content : null;

    private static bool IsPathParameter(JsonNode? parameter)
        => parameter is JsonObject described
            && described["in"] is JsonValue location
            && location.TryGetValue(out string? kind)
            && string.Equals(kind, "path", StringComparison.Ordinal);

    private async Task<IGenericResult> AddDataSets(
        ApiReferenceDocument document,
        string routePath,
        JsonObject template,
        IWorkspaceImplementationConfiguration workspace,
        CancellationToken cancellationToken)
    {
        var readable = await _readService.DataSets(workspace, cancellationToken).ConfigureAwait(false);
        if (readable.IsFailure || readable.Value is not { } dataSets)
            return readable;

        foreach (var dataSet in dataSets)
            document.AddOperation(PathFor(routePath, workspace.Name, dataSet.Name), "post", OperationFor(template, workspace.Name, dataSet));

        ApiReferenceLog.WorkspaceDescribed(Logger, workspace.Name, dataSets.Count);
        return GenericResult.Success();
    }
}
