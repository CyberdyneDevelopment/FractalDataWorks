using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Data;

/// <summary>
/// A view over the container that answers every logging request with a null logger and delegates
/// everything else.
/// </summary>
/// <remarks>
/// Why this exists: the connection a configuration gateway opens is built before any configuration
/// has been read, and building it must therefore not ask for a logger. Asking re-enters Serilog's
/// <c>AddSerilog</c> configure callback, which reads the logging domain's configuration, which is
/// read through the gateway being built — a cycle that parks the host with no exception and no log
/// line:
/// <code>
/// SerilogLoggingType configure callback
///   -> LoggingTypes resolves ILoggingConfigurationProvider
///     -> ConfigurationGatewayTypes builds the gateway
///       -> resolves the connection factory, which wants an ILogger
///         -> Serilog AddSerilog callback ...
/// </code>
/// The logger is the half that can give way. A logger genuinely needs configuration; the connection
/// that configuration is read through cannot wait for a logger to exist. So this one construction
/// takes <c>NullLogger</c>, which is the same sanctioned exception as
/// <c>logger ?? NullLogger&lt;T&gt;.Instance</c> — a deliberate choice at a site that sits INSIDE
/// logger construction, not a fallback for a missing value.
/// <para>
/// Scoped as narrowly as possible: it is used for exactly one <c>CreateInstance</c> call and never
/// registered, so nothing else can pick up a silent null logger.
/// </para>
/// </remarks>
internal sealed class LoggerlessServiceProvider : IServiceProvider
{
    private readonly IServiceProvider _inner;

    internal LoggerlessServiceProvider(IServiceProvider inner) => _inner = inner;

    public object? GetService(Type serviceType)
    {
        if (serviceType == typeof(ILoggerFactory))
            return NullLoggerFactory.Instance;

        if (serviceType.IsGenericType && serviceType.GetGenericTypeDefinition() == typeof(ILogger<>))
            return Activator.CreateInstance(
                typeof(NullLogger<>).MakeGenericType(serviceType.GenericTypeArguments[0]));

        return serviceType == typeof(ILogger) ? NullLogger.Instance : _inner.GetService(serviceType);
    }
}
