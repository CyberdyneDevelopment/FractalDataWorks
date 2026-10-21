namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>
/// One reason a component gave for rejecting rows through an error output, and how many rows it gave it for (FDW-1720).
/// </summary>
/// <param name="Reason">The reason, as the component stated it. It is the component's own wording and holds no row.</param>
/// <param name="Count">How many rows were rejected for it.</param>
public sealed record FlowRejectionReason(string Reason, long Count);
