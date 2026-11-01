namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>A path request that passed validation.</summary>
internal sealed record LineagePathQuery(string From, string To, int MaxDepth);
