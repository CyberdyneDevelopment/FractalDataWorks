using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Pages;

/// <summary>
/// TypeCollection for UI log severity levels.
/// Note: This is Fdw.UI.Abstractions.Pages.LogLevels, distinct from Microsoft.Extensions.Logging.LogLevel.
/// </summary>
[TypeCollection(typeof(LogLevelBase), typeof(ILogLevel), typeof(LogLevels))]
[ExcludeFromCodeCoverage]
public abstract partial class LogLevels : TypeCollectionBase<LogLevelBase, ILogLevel> { }
