using System;

namespace Fdw.Operations.Abstractions.Thresholds;

/// <summary>What a set of values is about (FDW-3936): the subject that reports them.</summary>
/// <param name="Kind">The kind of subject: Pipeline, Component, Connection, Schedule or DataSet.</param>
/// <param name="Name">The subject name; a component is "pipeline/component".</param>
/// <param name="SubjectId">The subject's Id in its domain table. Thresholds are attached to a subject by this Id, in the link table of its kind.</param>
/// <param name="TenantId">The tenant the subject belongs to. Required: an alert and a stored threshold belong to a tenant.</param>
/// <param name="RunId">The run the values were measured in, for a subject that runs; none otherwise. The consequences that act on a run need it.</param>
public sealed record ThresholdSubject(string Kind, string Name, Guid SubjectId, Guid TenantId, Guid? RunId);
