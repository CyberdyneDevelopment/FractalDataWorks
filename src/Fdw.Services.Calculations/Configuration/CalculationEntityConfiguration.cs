using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Calculations.Abstractions;

namespace Fdw.Services.Calculations.Configuration;

/// <summary>
/// Aggregate configuration for the <c>calc.CalculationEntity</c> table — the header plus its composed
/// child collections (Inputs, Steps→{Fields,Operands}) and polymorphic typed body (Formula/Windowed).
/// </summary>
/// <remarks>
/// Why: the keystone <c>ImplementationConfigurationProviderBase</c> composes the full aggregate on read
/// (ComposeChildren for the nav collections, ComposeTypedBody for <see cref="Configuration"/> dispatched
/// on <see cref="Implementation"/>) and cascade-saves it on write — there is no per-domain hand-assembly.
/// Named without the "Managed" suffix so the cascade FK derives correctly: Strip("Configuration") =>
/// "CalculationEntity" => child FK column "CalculationEntityId" (matches the DDL).
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration( ServiceCategory = "Calculation", ServiceType = "Entity", Temporal = true)]
public partial class CalculationEntityConfiguration : DomainConfigurationBase, IDomainConfiguration
{













}
