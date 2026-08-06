using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Calculations.Commands;
using Fdw.Services.Calculations.Configuration;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Fdw.Services.Calculations;

/// <summary>
/// Header configuration provider for the calculation domain. The full aggregate — Inputs, Steps→{Fields,
/// Operands}, and the polymorphic Formula/Windowed typed body — is composed on read and cascade-saved on
/// write entirely by the keystone <see cref="DefaultConfigurationProvider{TConfig,TCommand}"/>; there is no
/// per-domain hand-assembly. Typed providers are registered with this header via the inherited
/// <c>RegisterTypedProvider</c> in <see cref="DefaultCalculationServiceType"/> (dispatch on ServiceOptionType).
/// </summary>
public class CalculationConfigurationProvider : DefaultConfigurationProvider<CalculationEntityConfiguration, CalculationEntityConfigurationCommand>
{
    /// <summary>
    /// Registers the CalculationConfigurationProvider with DI, targeting this domain's own default
    /// location. To override, call <c>SetConfiguration</c> on the resolved singleton.
    /// </summary>
    /// <summary>Initializes a new instance of the <see cref="CalculationConfigurationProvider"/> class.</summary>
    public CalculationConfigurationProvider(
        ILogger<CalculationConfigurationProvider> logger,
        Lazy<IConfigurationGateway> lazyGateway,
        string dataStoreName = "ConfigurationDb",
        string pathName = "calc",
        Lazy<ICacheInvalidator?>? invalidator = null)
        : base(logger ?? NullLogger<CalculationConfigurationProvider>.Instance,
               lazyGateway,
               dataStoreName, pathName,
               invalidator)
    {
    }

    /// <summary>
    /// Persists the calculation aggregate (header + Inputs/Steps + typed body) via the keystone cascade.
    /// </summary>
    /// <remarks>
    /// Why: the base cascade saves the typed body via its ConfigurationCommand but does not set the body's
    /// parent FK (unlike collection children, whose FK the cascade derives from the parent type name). Stamp
    /// the typed body's logical CalculationEntityId here — reflection-free via the generated mapper — so the
    /// save translator resolves the physical CalculationEntityRowId on insert. Mirrors
    /// SecretManagerConfigurationProvider.Save stamping SecretManagerId. NO FALLBACKS — only stamps when a
    /// typed body is present and its mapper exists.
    /// </remarks>
    public override Task<IGenericResult<CalculationEntityConfiguration>> Save(
        CalculationEntityConfiguration record, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        if (record.Id == Guid.Empty)
            record.Id = Guid.CreateVersion7();

        // Why: stamp the typed body's logical FK directly via ICalculationTypedConfiguration — the body
        // shares the header's identity, and the save translator resolves the physical RowId on insert.
        if (record.Configuration is not null)
            record.Configuration.CalculationEntityId = record.Id;

        return base.Save(record, ct);
    }
}
