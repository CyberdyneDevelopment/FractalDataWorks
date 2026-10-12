using Microsoft.Extensions.Logging;

namespace Fdw.Web.RestEndpoints.Tests.Logging;

/// <summary>
/// Hands the same <see cref="RecordingLogger"/> to every category.
/// </summary>
/// <remarks>
/// The registration phases create their loggers per category from the factory they are given, so a
/// per-category logger would scatter one phase's narration across several recorders. One recorder
/// for all of them keeps the entries in the order they were emitted, which is what makes an
/// assertion about the sequence meaningful.
/// </remarks>
/// <param name="logger">The recorder every category gets.</param>
internal sealed class RecordingLoggerFactory(RecordingLogger logger) : ILoggerFactory
{
    /// <inheritdoc/>
    public void AddProvider(ILoggerProvider provider)
    {
        // Nothing to do: this factory has exactly one sink and it is the recorder above.
    }

    /// <inheritdoc/>
    public ILogger CreateLogger(string categoryName) => logger;

    /// <inheritdoc/>
    public void Dispose()
    {
        // The recorder holds a list, not a resource.
    }
}
