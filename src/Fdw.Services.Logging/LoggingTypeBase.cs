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
    protected LoggingTypeBase(string name, string sectionName, string displayName, string description)
        : base(name, sectionName, displayName, description)
    {
    }
}
