using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Services.Telemetry.Abstractions;
using Fdw.Services.Abstractions;
using Fdw.ServiceTypes;

namespace Fdw.Services.Telemetry;

/// <summary>
/// The base every telemetry option closes — Serilog and any sibling.
/// </summary>
/// <typeparam name="TService">The telemetry service the option's factory creates.</typeparam>
/// <typeparam name="TConfiguration">The implementation configuration it binds to.</typeparam>
/// <typeparam name="TFactory">The factory the option registers.</typeparam>
[ExcludeFromCodeCoverage(Justification = "Abstract base: constructor-only logic")]
public abstract class TelemetryTypeBase<TService, TConfiguration, TFactory>
    : ServiceTypeBase<TService, TFactory, TConfiguration>,
      ITelemetryType<TService, TConfiguration, TFactory>
    where TService : ITelemetryService
    where TConfiguration : class, IGenericConfiguration
    where TFactory : ITelemetryFactory<TService, TConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="TelemetryTypeBase{TService, TConfiguration, TFactory}"/> class.</summary>
    /// <param name="name">The option's discriminator within <see cref="TelemetryTypes"/>.</param>
    /// <param name="sectionName">The configuration section this option reads.</param>
    /// <param name="displayName">Human-readable name.</param>
    /// <param name="description">What this telemetry pipeline is for.</param>
    protected TelemetryTypeBase(string name, string sectionName, string displayName, string description)
        : base(name, sectionName, displayName, description)
    {
    }
}
