using System;
using Fdw.Results;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Fdw.ServiceTypes;

/// <summary>
/// Descriptor for a discovered ServiceTypeCollection domain, registered in the opt-in
/// <c>PlatformServices</c> registry (see <c>Fdw.Services.Registration</c>). Each descriptor exposes
/// the collection's identity plus its three-phase entry points, so the aggregate registry can invoke
/// every domain's Configure/Register/Initialize uniformly without reflection.
/// </summary>
public interface IServiceTypeCollection
{
    /// <summary>The category name (e.g. "Connection", "SecretManager"). Matches <c>ServiceCategory</c> on the generated collection.</summary>
    string ServiceCategory { get; }

    /// <summary>The CLR type of the generated ServiceTypeCollection (e.g. <c>typeof(ConnectionTypes)</c>).</summary>
    Type CollectionType { get; }

    /// <summary>
    /// The collection's phase-1 Configure entry point (e.g. <c>ConnectionTypes.Configure</c>), bound as
    /// a bare method-group delegate by the generator — never populated via reflection.
    /// </summary>
    Func<IHostApplicationBuilder, ILoggerFactory?, bool, IGenericResult<IHostApplicationBuilder>> Configure { get; }

    /// <summary>The collection's phase-2 Register entry point (e.g. <c>ConnectionTypes.Register</c>).</summary>
    Func<IHostApplicationBuilder, ILoggerFactory?, bool, IGenericResult<IHostApplicationBuilder>> Register { get; }

    /// <summary>The collection's phase-3 Initialize entry point (e.g. <c>ConnectionTypes.Initialize</c>).</summary>
    Func<IHost, ILoggerFactory?, bool, IGenericResult<IHost>> Initialize { get; }
}
