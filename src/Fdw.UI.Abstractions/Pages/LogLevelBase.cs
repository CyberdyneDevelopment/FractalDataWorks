using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.UI.Abstractions.Pages;

/// <summary>
/// Base class for UI log severity levels.
/// </summary>
// Why: pure TypeOption base — trivial pass-through constructor, no logic to test.
[ExcludeFromCodeCoverage]
public abstract class LogLevelBase : TypeOptionBase<int, LogLevelBase>, ILogLevel
{
    /// <summary>
    /// Initializes a new instance of <see cref="LogLevelBase"/>.
    /// </summary>
    protected LogLevelBase(int id, string name) : base(id, name) { }
}
