using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Services.Hosts.Abstractions;
using Fdw.Services.Abstractions;
using Fdw.ServiceTypes;

namespace Fdw.Services.Hosts;

/// <summary>
/// The base every hosting option closes — Serilog and any sibling.
/// </summary>
/// <typeparam name="TService">The hosting service the option's factory creates.</typeparam>
/// <typeparam name="TConfiguration">The implementation configuration it binds to.</typeparam>
/// <typeparam name="TFactory">The factory the option registers.</typeparam>
[ExcludeFromCodeCoverage(Justification = "Abstract base: constructor-only logic")]
public abstract class HostTypeBase<TService, TConfiguration, TFactory>
    : ServiceTypeBase<TService, TFactory, TConfiguration>,
      IHostType<TService, TConfiguration, TFactory>
    where TService : IHostService
    where TConfiguration : class, IGenericConfiguration
    where TFactory : IHostFactory<TService, TConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="HostTypeBase{TService, TConfiguration, TFactory}"/> class.</summary>
    /// <param name="name">The option's discriminator within <see cref="HostTypes"/>.</param>
    /// <param name="sectionName">The configuration section this option reads.</param>
    /// <param name="displayName">Human-readable name.</param>
    /// <param name="description">What this hosting pipeline is for.</param>
    protected HostTypeBase(string name, string sectionName, string displayName, string description)
        : base(name, sectionName, displayName, description)
    {
    }
}
