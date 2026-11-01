using System;
using System.Linq;
using System.Reflection;
using Fdw.Services.Data.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Endpoints.Tests;

/// <summary>
/// FDW-1092: a stand-in is a configuration row in the platform's own store, which the main data gateway refuses a
/// request over (FDW-1068). Every stand-in endpoint therefore reads and writes through the configuration gateway,
/// the way the domain providers do, and none of them is handed the request-facing <see cref="IDataGatewayProvider"/>.
/// </summary>
public sealed class StandInEndpointsGatewayTests
{
    public static TheoryData<Type> StandInEndpoints => new()
    {
        typeof(GetDataSetFieldStandInEndpointBase),
        typeof(GetDataSetStandInsEndpointBase),
        typeof(SetDataSetFieldStandInEndpointBase),
        typeof(ClearDataSetFieldStandInEndpointBase),
        typeof(PreviewStandInEndpointBase),
    };

    [Theory]
    [MemberData(nameof(StandInEndpoints))]
    public void Endpoint_takes_the_configuration_gateway_provider_and_not_the_main_one(Type endpoint)
    {
        var parameterTypes = endpoint
            .GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .SelectMany(constructor => constructor.GetParameters())
            .Select(parameter => parameter.ParameterType)
            .ToList();

        parameterTypes.ShouldContain(typeof(IConfigurationGatewayProvider));
        parameterTypes.ShouldNotContain(typeof(IDataGatewayProvider));
    }
}
