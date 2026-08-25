using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Services.Connections.Abstractions;

namespace Fdw.Services.Connections.TestDouble;

/// <summary>
/// The <c>MockConnection</c> <c>[ServiceTypeOption]</c> this assembly supplies to
/// <see cref="ConnectionTypes"/>.
/// </summary>
/// <remarks>
/// <para>
/// Why an option rather than a bare configuration class: a schema declaring
/// <c>"ServiceOptionType": "MockConnection"</c> is resolved by
/// <c>ConnectionTypes.ByName</c>, and a configuration type alone registers nothing. The
/// configuration used to carry only <c>[ManagedConfiguration]</c> and live in the test assembly, where
/// no module initializer could ever reach it — the generator scans referenced assemblies, never the
/// compilation it runs in — so the lookup failed on every run and the schema-load test could not pass.
/// </para>
/// <para>
/// Why a mock rather than a real connection: the tests assert that the loader resolves a connection's
/// typed body from its discriminator. Which connection it is does not matter, only that one is
/// registered and its body comes back strongly typed. Every real connection implementation lives in
/// reference-servicetypes, so reaching for one would give an FDW test a dependency on a downstream
/// repo purely to obtain a shape it never inspects.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[ServiceTypeOption(typeof(ConnectionTypes), OptionName)]
public sealed class MockConnectionType
    : ConnectionTypeBase<IGenericConnection, IMockConnectionFactory, MockConnectionConfiguration>
{
    /// <summary>The discriminator this option registers under.</summary>
    public const string OptionName = "MockConnection";

    /// <summary>Initializes a new instance of the <see cref="MockConnectionType"/> class.</summary>
    public MockConnectionType()
        : base(
            name: OptionName,
            sectionName: OptionName,
            displayName: "Mock (test)",
            description: "Test-owned connection whose typed body the schema-load tests resolve.",
            category: "Test")
    {
    }
}
