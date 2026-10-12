using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Data.Abstractions;
using Fdw.Data.DataStores.Abstractions;
using Fdw.Data.MsSql;
using Shouldly;
using IDataNodePath = Fdw.Data.DataStores.Abstractions.IDataPath;
using Xunit;

namespace Fdw.Data.MsSql.Tests.Paths;

public sealed class DatabasePathTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ThreePartConstructorSetsAllProperties()
    {
        var sut = new DatabasePath("Northwind", "dbo", "Customers");

        sut.Database.ShouldBe("Northwind");
        sut.Schema.ShouldBe("dbo");
        sut.ObjectName.ShouldBe("Customers");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void TwoPartConstructorDefaultsToDboSchema()
    {
        var sut = new DatabasePath("Northwind", "Customers");

        sut.Database.ShouldBe("Northwind");
        sut.Schema.ShouldBe("dbo");
        sut.ObjectName.ShouldBe("Customers");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void PathValueWithDatabaseIncludesAllThreeParts()
    {
        var sut = new DatabasePath("Northwind", "dbo", "Customers");
        sut.PathValue.ShouldBe("Northwind.dbo.Customers");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void PathValueWithoutDatabaseIncludesTwoParts()
    {
        var sut = new DatabasePath("", "dbo", "Customers");
        sut.PathValue.ShouldBe("dbo.Customers");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void PathValueWithNullDatabaseIncludesTwoParts()
    {
        var sut = new DatabasePath(null, "dbo", "Customers");
        sut.PathValue.ShouldBe("dbo.Customers");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void QuotedIdentifierWithDatabaseBracketsAllParts()
    {
        var sut = new DatabasePath("Northwind", "dbo", "Customers");
        sut.QuotedIdentifier.ShouldBe("[Northwind].[dbo].[Customers]");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void QuotedIdentifierWithoutDatabaseBracketsTwoParts()
    {
        var sut = new DatabasePath("", "dbo", "Customers");
        sut.QuotedIdentifier.ShouldBe("[dbo].[Customers]");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void SchemaQualifiedNameAlwaysReturnsTwoParts()
    {
        var sut = new DatabasePath("Northwind", "sales", "Orders");
        sut.SchemaQualifiedName.ShouldBe("[sales].[Orders]");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DomainIsSql()
    {
        var sut = new DatabasePath("db", "dbo", "table");
        sut.Domain.ShouldBe("Sql");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorThrowsForNullSchema()
    {
        Should.Throw<ArgumentNullException>(() => new DatabasePath("db", null!, "table"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorThrowsForNullObjectName()
    {
        Should.Throw<ArgumentNullException>(() => new DatabasePath("db", "dbo", null!));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ContainersIsEmptyByDefault()
    {
        var sut = new DatabasePath("db", "dbo", "table");
        sut.Containers.Count.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetContainerReturnsNullForEmptyName()
    {
        var sut = new DatabasePath("db", "dbo", "table");
        sut.GetContainer("").ShouldBeNull();
        sut.GetContainer(null!).ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ContainsContainerReturnsFalseForEmptyName()
    {
        var sut = new DatabasePath("db", "dbo", "table");
        sut.ContainsContainer("").ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ContainsContainerReturnsFalseWhenNotPresent()
    {
        var sut = new DatabasePath("db", "dbo", "table");
        sut.ContainsContainer("NonExistent").ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IDataPathIdCombinesAllParts()
    {
        var sut = new DatabasePath("Northwind", "dbo", "Customers");
        ((IDataNodePath)sut).Id.ShouldBe("Northwind.dbo.Customers");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IDataPathNameIsObjectName()
    {
        var sut = new DatabasePath("Northwind", "dbo", "Customers");
        ((IDataNodePath)sut).Name.ShouldBe("Customers");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IDataPathPathTypeIsDatabasePath()
    {
        var sut = new DatabasePath("db", "dbo", "table");
        ((IDataNodePath)sut).PathType.ShouldBe("DatabasePath");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IDataPathSegmentsContainsAllParts()
    {
        var sut = new DatabasePath("Northwind", "dbo", "Customers");
        var segments = ((IDataNodePath)sut).Segments;
        segments.Count.ShouldBe(3);
        segments[0].ShouldBe("Northwind");
        segments[1].ShouldBe("dbo");
        segments[2].ShouldBe("Customers");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IDataPathRequiresParametersIsFalse()
    {
        var sut = new DatabasePath("db", "dbo", "table");
        ((IDataNodePath)sut).RequiresParameters.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IDataPathResolveParametersReturnsSelf()
    {
        var sut = new DatabasePath("db", "dbo", "table");
        var resolved = ((IDataNodePath)sut).ResolveParameters(new Dictionary<string, object>());
        resolved.ShouldBe(sut);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IDataPathValidateParametersReturnsSuccess()
    {
        var sut = new DatabasePath("db", "dbo", "table");
        var result = ((IDataNodePath)sut).ValidateParameters(new Dictionary<string, object>());
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IDataPathGetParentReturnsNull()
    {
        var sut = new DatabasePath("db", "dbo", "table");
        ((IDataNodePath)sut).GetParent().ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IDataPathGetChildrenReturnsEmpty()
    {
        var sut = new DatabasePath("db", "dbo", "table");
        ((IDataNodePath)sut).GetChildren().ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IDataPathCombineReturnsSelf()
    {
        var sut = new DatabasePath("db", "dbo", "table");
        ((IDataNodePath)sut).Combine("anything").ShouldBe(sut);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void CustomSchemaGeneratesCorrectPaths()
    {
        var sut = new DatabasePath("ConfigDb", "cfg", "Connection");
        sut.QuotedIdentifier.ShouldBe("[ConfigDb].[cfg].[Connection]");
        sut.SchemaQualifiedName.ShouldBe("[cfg].[Connection]");
    }
}
