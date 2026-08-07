using Fdw.Collections;

namespace Fdw.Services.Data.Abstractions;

/// <summary>
/// Interface for schema discovery type options.
/// Each implementation represents a store-type-specific schema discoverer (e.g., MsSql, PostgreSql).
/// </summary>
/// <remarks>
/// Why no Register member here: registration is a phase, and the phases come from
/// <c>PhasedTypeOptionBase</c> with the same shape every registering option uses — a replaceable
/// <c>Registration(...)</c> body over <c>IHostApplicationBuilder</c>. Declaring a second, domain-local
/// <c>Register(IServiceCollection)</c> gave this domain its own contract for the same idea, and the raw
/// service collection with it.
/// </remarks>
public interface ISchemaDiscoveryType : ITypeOption<int, SchemaDiscoveryTypeBase>
{
}
