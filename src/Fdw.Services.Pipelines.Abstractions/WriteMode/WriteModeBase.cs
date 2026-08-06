using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.Services.Pipelines.Abstractions.WriteMode;

/// <summary>
/// Base class for write mode types using CRTP pattern.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class WriteModeBase : TypeOptionBase<int, WriteModeBase>, IWriteMode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WriteModeBase"/> class.
    /// </summary>
    protected WriteModeBase(int id, string name) : base(id, name) { }
}
