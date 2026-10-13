using System;
using System.Linq;
using System.Reflection;
using Fdw.Services.Data.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Endpoints.Tests;

/// <summary>
/// FDW-1115: the rows these endpoints read and write (field-mapping transform attachments, transformations, data set
/// samples, a field's history) are configuration rows in the store the host declared, which the main data gateway
/// refuses a request over (FDW-1068, DATA-20007). Each therefore goes through the configuration gateway, the way the
/// domain providers and the stand-in endpoints (FDW-1092) do, and none is handed the request-facing
/// <see cref="IDataGatewayProvider"/>. An endpoint over a user's own data store is not on this list and keeps the main gateway.
/// </summary>
public sealed class PlatformRowEndpointsGatewayTests
{
    public static TheoryData<Type> PlatformRowEndpoints => new()
    {
        typeof(SaveFieldMappingTransformationEndpointBase),
        typeof(DeleteFieldMappingTransformationEndpointBase),
        typeof(ListFieldMappingTransformationsEndpointBase),
        typeof(ReorderFieldMappingTransformationsEndpointBase),
        typeof(DeleteTransformationEndpointBase),
        typeof(ListDataSetSamplesEndpointBase),
        typeof(UpsertDataSetSampleEndpointBase),
        typeof(GetDataSetFieldHistoryEndpointBase),
    };

    [Theory]
    [MemberData(nameof(PlatformRowEndpoints))]
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
