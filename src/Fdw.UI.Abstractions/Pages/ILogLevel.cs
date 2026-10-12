using Fdw.Collections;

namespace Fdw.UI.Abstractions.Pages;

/// <summary>
/// Interface for UI log severity levels.
/// Note: This is Fdw.UI.Abstractions.Pages.ILogLevel, distinct from Microsoft.Extensions.Logging.LogLevel.
/// </summary>
public interface ILogLevel : ITypeOption<int, LogLevelBase> { }
