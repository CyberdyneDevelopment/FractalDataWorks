using Fdw.Configuration;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.Http.Abstractions;

/// <summary>
/// Context passed to HTTP protocol implementations containing configuration and resolved secrets.
/// </summary>
/// <remarks>
/// <para>
/// This context is built once at connection creation time by the factory and passed
/// to all protocol method calls. It captures:
/// <list type="bullet">
/// <item><description>Configuration - HTTP connection settings</description></item>
/// <item><description>LoggerFactory - For creating protocol-specific loggers</description></item>
/// <item><description>Resolved secrets - Certificates, passwords, API keys resolved at creation</description></item>
/// </list>
/// </para>
/// <para>
/// Secrets are resolved once during connection creation to avoid repeated lookups
/// on every request. This follows the same pattern as MsSqlProcessorContext.
/// </para>
/// </remarks>
/// <param name="Configuration">The HTTP connection configuration.</param>
/// <param name="LoggerFactory">The logger factory for creating protocol loggers.</param>
/// <param name="ResolvedCertificate">X.509 certificate resolved from secret manager (for WS-Security).</param>
/// <param name="ResolvedPassword">Password resolved from secret manager.</param>
/// <param name="ResolvedApiKey">API key resolved from secret manager.</param>
// Why: pure positional record (DTO), auto-generated properties only, no logic
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public readonly record struct HttpProtocolContext(
    IGenericConfiguration Configuration,
    ILoggerFactory LoggerFactory,
    X509Certificate2? ResolvedCertificate,
    string? ResolvedPassword,
    string? ResolvedApiKey);
