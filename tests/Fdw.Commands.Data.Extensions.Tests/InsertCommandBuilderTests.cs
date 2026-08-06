using System;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Extensions;
using Shouldly;
using Xunit;

namespace Fdw.Commands.Data.Extensions.Tests;

public sealed class InsertCommandBuilderTests
{
    private sealed class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void InsertSingleBuildsCorrectCommand()
    {
        var entity = new TestEntity { Id = 1, Name = "Acme" };

        var call = Insert.Into<TestEntity>("Customers")
            .DataStore("CRM")
            .Path("sales")
            .Value(entity);

        var cmd = (InsertCommand<TestEntity>)call.Command;

        call.Target.Container.ShouldBe("Customers");
        call.Target.DataStore.ShouldBe("CRM");
        call.Target.Path.ShouldBe("sales");
        cmd.Data.ShouldBe(entity);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void InsertSingleThrowsWhenDataStoreNotSpecified()
    {
        var entity = new TestEntity { Id = 1, Name = "Acme" };
        var builder = Insert.Into<TestEntity>("Customers")
            .Path("sales");

        Should.Throw<InvalidOperationException>(() => builder.Value(entity));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void InsertSingleThrowsWhenPathNotSpecified()
    {
        var entity = new TestEntity { Id = 1, Name = "Acme" };
        var builder = Insert.Into<TestEntity>("Customers")
            .DataStore("CRM");

        Should.Throw<InvalidOperationException>(() => builder.Value(entity));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void InsertSingleDataStoreThrowsForNull()
    {
        Should.Throw<ArgumentNullException>(() =>
            Insert.Into<TestEntity>("Customers").DataStore(null!));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void InsertSinglePathThrowsForNull()
    {
        Should.Throw<ArgumentNullException>(() =>
            Insert.Into<TestEntity>("Customers").Path(null!));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void InsertBatchBuildsCorrectCommand()
    {
        var entities = new[]
        {
            new TestEntity { Id = 1, Name = "A" },
            new TestEntity { Id = 2, Name = "B" }
        };

        var call = Insert.IntoMany<TestEntity>("Customers")
            .DataStore("CRM")
            .Path("sales")
            .Values(entities);

        call.Target.Container.ShouldBe("Customers");
        call.Target.DataStore.ShouldBe("CRM");
        call.Target.Path.ShouldBe("sales");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void InsertBatchThrowsWhenDataStoreNotSpecified()
    {
        var entities = new[] { new TestEntity { Id = 1, Name = "A" } };
        var builder = Insert.IntoMany<TestEntity>("Customers")
            .Path("sales");

        Should.Throw<InvalidOperationException>(() => builder.Values(entities));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void InsertBatchThrowsWhenPathNotSpecified()
    {
        var entities = new[] { new TestEntity { Id = 1, Name = "A" } };
        var builder = Insert.IntoMany<TestEntity>("Customers")
            .DataStore("CRM");

        Should.Throw<InvalidOperationException>(() => builder.Values(entities));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void InsertBatchDataStoreThrowsForNull()
    {
        Should.Throw<ArgumentNullException>(() =>
            Insert.IntoMany<TestEntity>("Customers").DataStore(null!));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void InsertBatchPathThrowsForNull()
    {
        Should.Throw<ArgumentNullException>(() =>
            Insert.IntoMany<TestEntity>("Customers").Path(null!));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void BulkInsertBuildsCorrectCommand()
    {
        var entities = new[]
        {
            new TestEntity { Id = 1, Name = "A" },
            new TestEntity { Id = 2, Name = "B" }
        };

        var call = Insert.Bulk<TestEntity>("Customers")
            .DataStore("CRM")
            .Path("sales")
            .Values(entities);

        call.Target.Container.ShouldBe("Customers");
        call.Target.DataStore.ShouldBe("CRM");
        call.Target.Path.ShouldBe("sales");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void BulkInsertThrowsWhenDataStoreNotSpecified()
    {
        var entities = new[] { new TestEntity { Id = 1, Name = "A" } };
        var builder = Insert.Bulk<TestEntity>("Customers")
            .Path("sales");

        Should.Throw<InvalidOperationException>(() => builder.Values(entities));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void BulkInsertThrowsWhenPathNotSpecified()
    {
        var entities = new[] { new TestEntity { Id = 1, Name = "A" } };
        var builder = Insert.Bulk<TestEntity>("Customers")
            .DataStore("CRM");

        Should.Throw<InvalidOperationException>(() => builder.Values(entities));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void BulkInsertDataStoreThrowsForNull()
    {
        Should.Throw<ArgumentNullException>(() =>
            Insert.Bulk<TestEntity>("Customers").DataStore(null!));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void BulkInsertPathThrowsForNull()
    {
        Should.Throw<ArgumentNullException>(() =>
            Insert.Bulk<TestEntity>("Customers").Path(null!));
    }
}
