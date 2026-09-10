using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Calculations.Abstractions;
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
/// The calculation domain's configuration provider. It reads the domain row, resolves the
/// implementation its <c>Implementation</c> value names, and attaches it; the subtree — Inputs,
/// Steps→{Fields, Operands} — is composed and cascade-saved by the base. Implementation providers are
/// registered with it via <c>Register</c> in <see cref="DefaultCalculationServiceType"/>.
/// </summary>
public class CalculationConfigurationProvider
    : ImplementationConfigurationProviderBase<DomainConfiguration, ICalculationEntityImplementationConfiguration, CalculationEntityConfigurationCommand>
{

    /// <summary>
    /// Registers the CalculationConfigurationProvider with DI, targeting this domain's own default
    /// location. To override, call <c>SetConfiguration</c> on the resolved singleton.
    /// </summary>
    /// <summary>Initializes a new instance of the <see cref="CalculationConfigurationProvider"/> class.</summary>
    public CalculationConfigurationProvider(
        ILogger<CalculationConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName = "calc")
        : base(logger ?? NullLogger<CalculationConfigurationProvider>.Instance,
               gatewayProvider,
               dataStoreName, pathName)
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
    protected override Task<IGenericResult<CalculationEntityConfiguration>> WriteRow(
        CalculationEntityConfiguration record, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        if (record.Id == Guid.Empty)
            record.Id = Guid.CreateVersion7();

        if (record.Configuration is not null)
            record.Configuration.CalculationEntityId = record.Id;

        return base.WriteRow(record, ct);
    }
}
