using System;
using System.Collections.Generic;
using Fdw.Results;
using Fdw.Services.Connections;
using Fdw.Services.Data;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Endpoints.Tests;

/// <summary>
/// Unit tests for the pure request-mapping logic in <see cref="AddDataStoreContainerEndpointBase"/> --
/// field construction and container-key resolution. Exercised directly, bypassing DataStoreConfigurationProvider
/// (sealed, so it cannot be mocked and is never invoked here).
/// </summary>
[Trait("Priority", "P1")]
[Trait("Category", "Configuration")]
public class AddDataStoreContainerEndpointTests
{
    private sealed class Endpoint : AddDataStoreContainerEndpointBase
    {
        // Never calls Create(), the only method that touches _dataStoreProvider, so a gateway
        // provider that resolves nothing real is enough to satisfy the constructor.
        public Endpoint() : base(
            NullLogger<AddDataStoreContainerEndpointBase>.Instance,
            new DataStoreConfigurationProvider(
                NullLogger<DataStoreConfigurationProvider>.Instance,
                Mock.Of<IConfigurationGatewayProvider>(),
                "TestStore", "dbo", "DataStore"))
        {
        }

        public static List<DataContainerFieldConfiguration> CallBuildFields(
            IEnumerable<CreateDataStoreContainerFieldRequest> fields) => BuildFields(fields);

        public IGenericResult<List<DataContainerKeyConfiguration>> CallBuildKeys(
            AddDataStoreContainerRequest request, DataContainerConfiguration container, IDataStoreImplementationConfiguration store) =>
            BuildKeys(request, container, store);

        public static List<string> CallKeyFieldNames(DataContainerConfiguration container, string keyTypeId) =>
            KeyFieldNames(container, keyTypeId);
    }

    private static DataStoreImplementationConfiguration EmptyStore() => new()
    {
        Name = "TestStore",
        Domain = "DataStore",
        Implementation = "MsSql",
        Paths = [],
    };

    [Fact]
    public void BuildFieldsAssignsIdAndMapsLengthFacets()
    {
        var request = new List<CreateDataStoreContainerFieldRequest>
        {
            new() { Name = "Amount", DataType = "Decimal", MaxLength = null, Precision = 18, Scale = 2 },
        };

        var fields = Endpoint.CallBuildFields(request);

        fields.Count.ShouldBe(1);
        fields[0].Id.ShouldNotBe(Guid.Empty);
        fields[0].Precision.ShouldBe(18);
        fields[0].Scale.ShouldBe(2);
    }

    [Fact]
    public void BuildKeysFailsOnUnknownKeyType()
    {
        var container = new DataContainerConfiguration { Name = "Orders", Fields = [] };
        var request = new AddDataStoreContainerRequest
        {
            Name = "TestStore",
            Keys = [new CreateDataStoreContainerKeyRequest { Name = "PK_Orders", TypeId = "NotAKeyType" }],
        };

        var result = new Endpoint().CallBuildKeys(request, container, EmptyStore());

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public void BuildKeysFailsWhenForeignKeyOmitsReference()
    {
        var container = new DataContainerConfiguration { Name = "Orders", Fields = [] };
        var request = new AddDataStoreContainerRequest
        {
            Name = "TestStore",
            Keys = [new CreateDataStoreContainerKeyRequest { Name = "FK_Customer", TypeId = "Foreign" }],
        };

        var result = new Endpoint().CallBuildKeys(request, container, EmptyStore());

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public void BuildKeysFailsWhenKeyFieldNameIsNotOnTheContainer()
    {
        var container = new DataContainerConfiguration { Name = "Orders", Fields = [] };
        var request = new AddDataStoreContainerRequest
        {
            Name = "TestStore",
            Keys =
            [
                new CreateDataStoreContainerKeyRequest
                {
                    Name = "PK_Orders",
                    TypeId = "Surrogate",
                    KeyFields = [new CreateDataStoreContainerKeyFieldRequest { FieldName = "DoesNotExist", Ordinal = 0 }],
                },
            ],
        };

        var result = new Endpoint().CallBuildKeys(request, container, EmptyStore());

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public void BuildKeysFailsWhenReferencedContainerIsNotInTheStore()
    {
        var idField = new DataContainerFieldConfiguration { Id = Guid.CreateVersion7(), Name = "CustomerId" };
        var container = new DataContainerConfiguration { Name = "Orders", Fields = [idField] };
        var request = new AddDataStoreContainerRequest
        {
            Name = "TestStore",
            Keys =
            [
                new CreateDataStoreContainerKeyRequest
                {
                    Name = "FK_Customer",
                    TypeId = "Foreign",
                    ReferencedContainerName = "Customers",
                    ReferencedKeyName = "PK_Customers",
                    KeyFields = [new CreateDataStoreContainerKeyFieldRequest { FieldName = "CustomerId", Ordinal = 0 }],
                },
            ],
        };

        var result = new Endpoint().CallBuildKeys(request, container, EmptyStore());

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public void BuildKeysResolvesSurrogateKeyFieldByName()
    {
        var idField = new DataContainerFieldConfiguration { Id = Guid.CreateVersion7(), Name = "OrderId" };
        var container = new DataContainerConfiguration { Name = "Orders", Fields = [idField] };
        var request = new AddDataStoreContainerRequest
        {
            Name = "TestStore",
            Keys =
            [
                new CreateDataStoreContainerKeyRequest
                {
                    Name = "PK_Orders",
                    TypeId = "Surrogate",
                    KeyFields = [new CreateDataStoreContainerKeyFieldRequest { FieldName = "OrderId", Ordinal = 0 }],
                },
            ],
        };

        var result = new Endpoint().CallBuildKeys(request, container, EmptyStore());

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Count.ShouldBe(1);
        result.Value![0].KeyFields.Count.ShouldBe(1);
        result.Value![0].KeyFields[0].DataContainerFieldId.ShouldBe(idField.Id);
        // data.DataContainerKeyField.KeyType is NOT NULL (FDW-853): a key field must carry its
        // owning key's type, not just the field it names.
        result.Value![0].KeyFields[0].KeyType.ShouldBe("Surrogate");
    }

    [Fact]
    public void BuildKeysResolvesForeignKeyReferenceWithinTheSameStore()
    {
        var customerIdField = new DataContainerFieldConfiguration { Id = Guid.CreateVersion7(), Name = "Id" };
        var customerKey = new DataContainerKeyConfiguration { Id = Guid.CreateVersion7(), Name = "PK_Customers", TypeId = "PrimaryKey" };
        var customersContainer = new DataContainerConfiguration
        {
            Name = "Customers",
            Fields = [customerIdField],
            Keys = [customerKey],
        };

        var fkField = new DataContainerFieldConfiguration { Id = Guid.CreateVersion7(), Name = "CustomerId" };
        var ordersContainer = new DataContainerConfiguration { Name = "Orders", Fields = [fkField] };

        var store = EmptyStore();
        store.Paths = [new DataPathConfiguration { Name = "dbo", Containers = [customersContainer] }];

        var request = new AddDataStoreContainerRequest
        {
            Name = "TestStore",
            Keys =
            [
                new CreateDataStoreContainerKeyRequest
                {
                    Name = "FK_Customer",
                    TypeId = "Foreign",
                    ReferencedContainerName = "Customers",
                    ReferencedKeyName = "PK_Customers",
                    KeyFields = [new CreateDataStoreContainerKeyFieldRequest { FieldName = "CustomerId", Ordinal = 0 }],
                },
            ],
        };

        var result = new Endpoint().CallBuildKeys(request, ordersContainer, store);

        result.IsSuccess.ShouldBeTrue();
        result.Value![0].ReferencedKeyId.ShouldBe(customerKey.Id);
    }

    [Fact]
    public void KeyFieldNamesReturnsOnlyTheRequestedKeyType()
    {
        var surrogateField = new DataContainerFieldConfiguration { Id = Guid.CreateVersion7(), Name = "SurrogateId" };
        var naturalField = new DataContainerFieldConfiguration { Id = Guid.CreateVersion7(), Name = "OrderNumber" };
        var container = new DataContainerConfiguration
        {
            Name = "Orders",
            Fields = [surrogateField, naturalField],
            Keys =
            [
                new DataContainerKeyConfiguration
                {
                    Name = "PK_Orders",
                    TypeId = "Surrogate",
                    KeyFields = [new DataContainerKeyFieldConfiguration { DataContainerFieldId = surrogateField.Id }],
                },
                new DataContainerKeyConfiguration
                {
                    Name = "UX_Orders_OrderNumber",
                    TypeId = "Natural",
                    KeyFields = [new DataContainerKeyFieldConfiguration { DataContainerFieldId = naturalField.Id }],
                },
            ],
        };

        Endpoint.CallKeyFieldNames(container, "Surrogate").ShouldBe(["SurrogateId"]);
        Endpoint.CallKeyFieldNames(container, "Natural").ShouldBe(["OrderNumber"]);
    }
}
