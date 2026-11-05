using System.Collections.Generic;

namespace Fdw.Data.Abstractions.Tests.Containers;

/// <summary>
/// The field a page of a container's rows follows when the query states no ordering is the one the container declares: the field of its
/// primary key, as a key of a data container or as the surrogate key in its metadata. A container that declares none answers nothing, and
/// the translator refuses the page (FDW-3454).
/// </summary>
public sealed class DeclaredOrderFieldNameTests
{
    private static IContainerKey PrimaryKeyOn(string fieldName)
    {
        var local = new Mock<IDataField>();
        local.Setup(f => f.Name).Returns(fieldName);
        var keyField = new Mock<IContainerKeyField>();
        keyField.Setup(k => k.LocalField).Returns(local.Object);
        var key = new Mock<IContainerKey>();
        key.Setup(k => k.KeyType).Returns(new PrimaryKeyType());
        key.Setup(k => k.KeyFields).Returns([keyField.Object]);
        return key.Object;
    }

    private static Mock<IDataContainer> Container(IContainerKey[] keys, string? surrogateKeyField)
    {
        var container = new Mock<IDataContainer>();
        container.Setup(c => c.Keys).Returns(keys);
        var storage = container.As<IStorageContainer>();
        storage.Setup(c => c.Metadata).Returns(
            surrogateKeyField is null ? new Dictionary<string, object>() : new Dictionary<string, object> { ["SurrogateKeyField"] = surrogateKeyField });
        return container;
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AContainerWithAPrimaryKeyDeclaresItsFieldAsTheOrder()
    {
        var container = Container([PrimaryKeyOn("Id")], surrogateKeyField: null);

        container.Object.GetDeclaredOrderFieldName().ShouldBe("Id");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AContainerWithOnlyASurrogateKeyInItsMetadataDeclaresThatFieldAsTheOrder()
    {
        var container = Container([], surrogateKeyField: "RowId");

        container.Object.GetDeclaredOrderFieldName().ShouldBe("RowId");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ADeclaredPrimaryKeyOutranksTheSurrogateKeyInTheMetadata()
    {
        var container = Container([PrimaryKeyOn("Id")], surrogateKeyField: "RowId");

        container.Object.GetDeclaredOrderFieldName().ShouldBe("Id");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AContainerThatDeclaresNoKeyAnswersNothingInsteadOfItsFirstField()
    {
        var container = Container([], surrogateKeyField: null);

        container.Object.GetDeclaredOrderFieldName().ShouldBeNull();
    }
}
