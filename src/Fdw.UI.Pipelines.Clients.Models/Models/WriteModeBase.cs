using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Pipelines.Clients.Models;

/// <summary>
/// Base class for data write modes.
/// </summary>
public abstract class WriteModeBase : TypeOptionBase<int, WriteModeBase>, IWriteMode
{
    /// <summary>
    /// Initializes a new instance of <see cref="WriteModeBase"/>.
    /// </summary>
    protected WriteModeBase(int id, string name) : base(id, name) { }
}
