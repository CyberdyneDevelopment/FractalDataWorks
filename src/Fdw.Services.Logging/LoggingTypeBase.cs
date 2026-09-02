using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Services.Logging.Abstractions;
using Fdw.Services.Abstractions;
using Fdw.ServiceTypes;

namespace Fdw.Services.Logging;

/// <summary>
/// The base every logging option closes — Serilog and any sibling.
/// </summary>
/// <typeparam name="TService">The logging service the option's factory creates.</typeparam>
/// <typeparam name="TConfiguration">The implementation configuration it binds to.</typeparam>
/// <typeparam name="TFactory">The factory the option registers.</typeparam>
[ExcludeFromCodeCoverage(Justification = "Abstract base: constructor-only logic")]
public abstract class LoggingTypeBase<TService, TConfiguration, TFactory>
    : ServiceTypeBase<TService, TFactory, TConfiguration>,
      ILoggingType<TService, TConfiguration, TFactory>
    where TService : ILoggingService
    where TConfiguration : class, IGenericConfiguration
    where TFactory : ILoggingFactory<TService, TConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="LoggingTypeBase{TService, TConfiguration, TFactory}"/> class.</summary>
    /// <param name="name">The option's discriminator within <see cref="LoggingTypes"/>.</param>
    /// <param name="sectionName">The configuration section this option reads.</param>
    /// <param name="displayName">Human-readable name.</param>
    /// <param name="description">What this logging pipeline is for.</param>
    /// <remarks>
    /// The store is named here and not left to the caller for the same reason hosting names its
    /// own: logging has to come up before the platform store is reachable, so its configuration
    /// lives on the file-backed server tier. ServiceTypeBase defaults all three to the empty
    /// string, so an option that does not pass them reads from no store at all -- which is what
    /// every logging option was doing.
    /// </remarks>
    protected LoggingTypeBase(string name, string sectionName, string displayName, string description)
        : base(name,
               sectionName,
               displayName,
               description,
               category: "Logging",
               defaultDataStoreName: "ServerConfiguration",
               defaultPathName: "log",
               defaultContainerName: name)
    {
    }
}
