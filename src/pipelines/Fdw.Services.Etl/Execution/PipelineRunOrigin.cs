using System;

namespace Fdw.Services.Etl.Execution;

/// <summary>
/// What a run's record says about where the run came from (FDW-6519): the person it acted as, the run mode that ran it, the host process and the slot of that host.
/// </summary>
/// <remarks>
/// Four facts, each its own value, so a person who filters runs by host or by slot never splits a text. <see cref="ActingPersonId"/> and <see cref="ExecutingSlot"/>
/// are null when the fact is absent (a run that acts as a tenant, a host off a slot); <see cref="RunMode"/> and <see cref="ExecutingHost"/> are always stated:
/// <see cref="PipelineRunOrigins"/> refuses to make an origin without them.
/// </remarks>
public sealed class PipelineRunOrigin
{
    /// <summary>Initializes a new instance of the <see cref="PipelineRunOrigin"/> class.</summary>
    /// <param name="actingPersonId">The person the run acts as, or null for a run that acts as a tenant.</param>
    /// <param name="runMode">The name of the run mode that ran it.</param>
    /// <param name="executingHost">The application name of the host process that ran it.</param>
    /// <param name="executingSlot">The slot's own name as the slot file states it, or null for a host off a slot.</param>
    internal PipelineRunOrigin(Guid? actingPersonId, string runMode, string executingHost, string? executingSlot)
    {
        ActingPersonId = actingPersonId;
        RunMode = runMode;
        ExecutingHost = executingHost;
        ExecutingSlot = executingSlot;
    }

    /// <summary>Gets the person the run acts as, or null for a run that acts as a tenant.</summary>
    public Guid? ActingPersonId { get; }

    /// <summary>Gets the name of the run mode that ran it.</summary>
    public string RunMode { get; }

    /// <summary>Gets the application name of the host process that ran it.</summary>
    public string ExecutingHost { get; }

    /// <summary>Gets the slot's own name as the slot file states it, or null for a host off a slot.</summary>
    public string? ExecutingSlot { get; }
}
