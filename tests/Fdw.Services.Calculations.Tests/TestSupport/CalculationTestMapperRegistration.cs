using System.Runtime.CompilerServices;
using Fdw.Data.Abstractions.Mappers.PocoMappers;

namespace Fdw.Services.Calculations.Tests.TestSupport;

/// <summary>Registers the hand-written mappers this project's test records need, once per test process.</summary>
/// <remarks>
/// The mapper collection freezes on its first read, and a member registered after that throws, so a mapper
/// registered from a test body works only when that test happens to run first. A module initializer runs when
/// this assembly loads, before any test can read the collection, whatever order the tests run in. Only records
/// defined in this assembly are registered here; the generator registers every other.
/// </remarks>
internal static class CalculationTestMapperRegistration
{
    [ModuleInitializer]
    internal static void Register() => PocoMapperCollection.RegisterMember(new UnregisteredCalculationConfigurationPocoMapper());
}
