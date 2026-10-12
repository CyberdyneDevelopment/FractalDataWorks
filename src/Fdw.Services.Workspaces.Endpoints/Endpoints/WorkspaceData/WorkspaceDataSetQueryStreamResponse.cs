using System;
using System.Text.Json.Serialization;
using Fdw.Services.Data.Endpoints;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>Response for <c>POST /{WorkspaceName}/{DataSetName}</c> when no page limit applies: every row, named with the workspace it was made through.</summary>
public sealed class WorkspaceDataSetQueryStreamResponse : DataQueryStreamResponse
{
    /// <summary>Initializes a new instance of the <see cref="WorkspaceDataSetQueryStreamResponse"/> class.</summary>
    /// <param name="workspaceName">The workspace the query was made through.</param>
    /// <param name="stream">The opened stream whose rows are written.</param>
    /// <param name="skip">The rows skipped.</param>
    public WorkspaceDataSetQueryStreamResponse(string workspaceName, DataQueryStream stream, int skip)
        : base(stream, skip)
    {
        WorkspaceName = workspaceName ?? throw new ArgumentNullException(nameof(workspaceName));
    }

    /// <summary>Gets the workspace the query was made through.</summary>
    /// <remarks>Ordered before every property of the base, so it leads the response as it does the paged one.</remarks>
    [JsonPropertyOrder(-1)]
    public string WorkspaceName { get; }
}
