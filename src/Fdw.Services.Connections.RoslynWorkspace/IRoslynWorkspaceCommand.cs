namespace Fdw.Services.Connections.RoslynWorkspace;

/// <summary>
/// Marker interface for the native RoslynWorkspace command type used by
/// <c>ConnectionBase&lt;TCommand, ...&gt;</c>.
/// </summary>
/// <remarks>
/// No concrete RoslynWorkspace DataGateway commands ship in 1.1.1 — this marker exists only
/// to satisfy the <c>ConnectionBase</c> generic constraint. Commands are deferred to 1.2.0
/// alongside the broader DataGateway-vs-Connector architectural decision.
/// Connectors call <see cref="Services.Connections.RoslynWorkspace.Abstractions.IRoslynWorkspaceClient"/>
/// directly per the §1.1 canary experiment.
/// </remarks>
public interface IRoslynWorkspaceCommand
{
}
