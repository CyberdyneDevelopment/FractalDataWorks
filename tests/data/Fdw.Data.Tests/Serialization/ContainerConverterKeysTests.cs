using System;
using System.Collections.Generic;
using System.Text.Json;
using Fdw.Data.Abstractions;
using Fdw.Data.Serialization;
using Fdw.Data.Serialization.Converters;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Data.Tests.Serialization;

/// <summary>
/// The container JSON form carries no keys (FDW-6309). A container that declares keys is refused, naming the container and the key,
/// rather than written without them and read back as a container with none, and the serialization container does not answer an empty
/// list that would read as a container with no keys.
/// </summary>
public sealed class ContainerConverterKeysTests
{
    private static readonly JsonSerializerOptions Options = new() { Converters = { new ContainerConverter() } };

    private static IContainerKey Key(string name)
    {
        var key = new Mock<IContainerKey>();
        key.Setup(k => k.KeyName).Returns(name);
        return key.Object;
    }

    private static IStorageContainer Container(params IContainerKey[] keys)
    {
        var container = new Mock<IStorageContainer>();
        container.Setup(c => c.Name).Returns("OrderLines");
        container.Setup(c => c.Keys).Returns(keys);
        return container.Object;
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AContainerThatDeclaresAKeyIsRefusedNamingTheContainerAndTheKey()
    {
        var thrown = Should.Throw<JsonException>(() => JsonSerializer.Serialize(Container(Key("PK_OrderLines")), Options));

        thrown.Message.ShouldContain("OrderLines");
        thrown.Message.ShouldContain("PK_OrderLines");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AContainerThatDeclaresSeveralKeysSaysHowManyMoreThereAre()
    {
        var thrown = Should.Throw<JsonException>(() => JsonSerializer.Serialize(Container(Key("PK_OrderLines"), Key("FK_Orders"), Key("FK_Items")), Options));

        thrown.Message.ShouldContain("and 2 more");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ASerializationContainerDoesNotAnswerAnEmptyListForItsKeys()
    {
        var serialized = new SerializedContainer
        {
            Name = "OrderLines",
            ContainerTypeName = "Table",
            FormatTypeName = "Tabular",
            PathTypeName = "DatabasePath",
            PathValue = "dbo",
            Schema = new ContainerSchema { Fields = [] },
            SupportedOperations = [],
            Metadata = new Dictionary<string, object>(),
        };

        Should.Throw<NotSupportedException>(() => ((IStorageContainer)serialized).Keys);
    }
}
