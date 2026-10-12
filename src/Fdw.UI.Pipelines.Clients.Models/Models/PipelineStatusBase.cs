using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Pipelines.Clients.Models;

/// <summary>
/// Base class for pipeline lifecycle statuses.
/// </summary>
public abstract class PipelineStatusBase : TypeOptionBase<PipelineStatusBase>, IPipelineStatus
{
    /// <summary>
    /// Initializes a new instance of <see cref="PipelineStatusBase"/>.
    /// </summary>
    protected PipelineStatusBase(string name) : base(name) { }
}
