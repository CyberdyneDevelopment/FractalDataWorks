using System;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>
/// The workspace a data set sits in, as the map names it on the data set's node (FDW-4345).
/// </summary>
/// <param name="Id">The workspace's id.</param>
/// <param name="Name">The workspace's name.</param>
internal sealed record MapWorkspaceRef(Guid Id, string Name);
