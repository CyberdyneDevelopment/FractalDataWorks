using System;
using System.Collections.Generic;
using Fdw.Schema;
using Fdw.Schema.Indexes;
using Fdw.Schema.Keys;
using Fdw.Schema.Properties;
using Fdw.Schema.Schemas;

namespace Fdw.Schema.Abstractions.Tests;

public sealed class SchemaDefinitionBaseTests
{
    private sealed class TestProperty : IPropertyDefinition
    {
        public required string Name { get; init; }
        public required IPropertyRole Role { get; init; }
        public bool IsRequired { get; init; }
        public string? Description { get; init; }
        public IReadOnlyDictionary<string, object>? Metadata { get; init; }
    }

    private sealed class TestSchema : SchemaDefinitionBase<TestProperty>
    {
        public TestSchema(
            string name,
            IReadOnlyList<TestProperty> properties,
            IDataLayout layout,
            string? description = null,
            IKeyDefinition<TestProperty>? surrogateKey = null,
            IKeyDefinition<TestProperty>? naturalKey = null,
            IReadOnlyList<IIndexDefinition<TestProperty>>? indexes = null,
            IReadOnlyList<ISchemaDefinition<TestProperty>>? children = null,
            string? pathExpression = null)
            : base(name, properties, layout, description, surrogateKey, naturalKey, indexes, children, pathExpression)
        {
        }
    }

    private static IDataLayout CreateLayout()
    {
        return DataLayouts.ByName("Tabular");
    }

    private static IPropertyRole CreateRole(string name = "Attribute")
    {
        return PropertyRoles.ByName(name);
    }

    private TestProperty CreateProperty(string name, string roleName = "Attribute")
    {
        return new TestProperty { Name = name, Role = CreateRole(roleName) };
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsAllProperties()
    {
        var props = new List<TestProperty>
        {
            CreateProperty("Id", "Surrogate"),
            CreateProperty("Name")
        };
        var layout = CreateLayout();
        var key = new KeyDefinition<TestProperty>(
            [new KeyMember(0, "Id")], "PK_Test");

        var schema = new TestSchema("Customers", props, layout,
            description: "Customer table",
            surrogateKey: key,
            pathExpression: "dbo.Customers");

        schema.Name.ShouldBe("Customers");
        schema.Properties.Count.ShouldBe(2);
        schema.Layout.ShouldBe(layout);
        schema.Description.ShouldBe("Customer table");
        schema.SurrogateKey.ShouldBe(key);
        schema.PathExpression.ShouldBe("dbo.Customers");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorThrowsOnNullName()
    {
        var props = new List<TestProperty> { CreateProperty("Id") };

        Should.Throw<ArgumentException>(() =>
            new TestSchema(null!, props, CreateLayout()));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorThrowsOnEmptyName()
    {
        var props = new List<TestProperty> { CreateProperty("Id") };

        Should.Throw<ArgumentException>(() =>
            new TestSchema("", props, CreateLayout()));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorThrowsOnWhitespaceName()
    {
        var props = new List<TestProperty> { CreateProperty("Id") };

        Should.Throw<ArgumentException>(() =>
            new TestSchema("   ", props, CreateLayout()));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorThrowsOnNullProperties()
    {
        Should.Throw<ArgumentNullException>(() =>
            new TestSchema("Test", null!, CreateLayout()));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorThrowsOnNullLayout()
    {
        var props = new List<TestProperty> { CreateProperty("Id") };

        Should.Throw<ArgumentNullException>(() =>
            new TestSchema("Test", props, null!));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IndexesDefaultsToEmpty()
    {
        var props = new List<TestProperty> { CreateProperty("Id") };

        var schema = new TestSchema("Test", props, CreateLayout());

        schema.Indexes.ShouldNotBeNull();
        schema.Indexes.Count.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ChildrenDefaultsToNull()
    {
        var props = new List<TestProperty> { CreateProperty("Id") };

        var schema = new TestSchema("Test", props, CreateLayout());

        schema.Children.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetReturnsMatchingProperty()
    {
        var props = new List<TestProperty>
        {
            CreateProperty("Id", "Surrogate"),
            CreateProperty("Name"),
            CreateProperty("Email")
        };

        var schema = new TestSchema("Test", props, CreateLayout());

        var result = schema.Get("Name");

        result.ShouldNotBeNull();
        result!.Name.ShouldBe("Name");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetIsCaseInsensitive()
    {
        var props = new List<TestProperty>
        {
            CreateProperty("CustomerName")
        };

        var schema = new TestSchema("Test", props, CreateLayout());

        schema.Get("customername").ShouldNotBeNull();
        schema.Get("CUSTOMERNAME").ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetReturnsDefaultWhenNotFound()
    {
        var props = new List<TestProperty> { CreateProperty("Id") };

        var schema = new TestSchema("Test", props, CreateLayout());

        schema.Get("NonExistent").ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetReturnsDefaultForNullInput()
    {
        var props = new List<TestProperty> { CreateProperty("Id") };

        var schema = new TestSchema("Test", props, CreateLayout());

        schema.Get((string)null!).ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetReturnsDefaultForEmptyInput()
    {
        var props = new List<TestProperty> { CreateProperty("Id") };

        var schema = new TestSchema("Test", props, CreateLayout());

        schema.Get("").ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetReturnsMatchingProperties()
    {
        var props = new List<TestProperty>
        {
            CreateProperty("Id", "Surrogate"),
            CreateProperty("Name"),
            CreateProperty("Email")
        };

        var schema = new TestSchema("Test", props, CreateLayout());
        var attributeRole = CreateRole("Attribute");

        var result = schema.Get(attributeRole);

        result.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetReturnsEmptyWhenNoMatch()
    {
        var props = new List<TestProperty>
        {
            CreateProperty("Name"),
            CreateProperty("Email")
        };

        var schema = new TestSchema("Test", props, CreateLayout());
        var surrogateRole = CreateRole("Surrogate");

        var result = schema.Get(surrogateRole);

        result.Count.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetThrowsOnNullRole()
    {
        var props = new List<TestProperty> { CreateProperty("Id") };
        var schema = new TestSchema("Test", props, CreateLayout());

        Should.Throw<ArgumentNullException>(() => schema.Get((IPropertyRole)null!));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IndexDefinitionStoresProperties()
    {
        var members = new List<IndexMember>
        {
            new(0, "Name"),
            new(1, "Email", IsDescending: true)
        };

        var index = new IndexDefinition<TestProperty>(
            "IX_Test_NameEmail", members,
            isUnique: true, isClustered: false,
            includeColumns: new List<string> { "Phone" },
            filterPredicate: "IsActive = 1");

        index.Name.ShouldBe("IX_Test_NameEmail");
        index.Members.Count.ShouldBe(2);
        index.IsUnique.ShouldBeTrue();
        index.IsClustered.ShouldBeFalse();
        index.IncludeColumns.ShouldNotBeNull();
        index.IncludeColumns!.Count.ShouldBe(1);
        index.FilterPredicate.ShouldBe("IsActive = 1");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void KeyDefinitionIsCompositeWhenMultipleMembers()
    {
        var members = new List<KeyMember>
        {
            new(0, "TenantId"),
            new(1, "UserId")
        };

        var key = new KeyDefinition<TestProperty>(members, "PK_Test");

        key.IsComposite.ShouldBeTrue();
        key.Name.ShouldBe("PK_Test");
        key.Members.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void KeyDefinitionIsNotCompositeWhenSingleMember()
    {
        var key = new KeyDefinition<TestProperty>(
            [new KeyMember(0, "Id")], "PK_Test");

        key.IsComposite.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IndexMemberRecordEquality()
    {
        var a = new IndexMember(0, "Name", false);
        var b = new IndexMember(0, "Name", false);
        var c = new IndexMember(1, "Name", true);

        a.ShouldBe(b);
        a.ShouldNotBe(c);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void KeyMemberRecordEquality()
    {
        var a = new KeyMember(0, "Id");
        var b = new KeyMember(0, "Id");
        var c = new KeyMember(1, "Name");

        a.ShouldBe(b);
        a.ShouldNotBe(c);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void SchemaWithChildSchemas()
    {
        var childProps = new List<TestProperty> { CreateProperty("DetailId") };
        var childSchema = new TestSchema("OrderDetails", childProps, CreateLayout());

        var parentProps = new List<TestProperty> { CreateProperty("OrderId") };
        var parentSchema = new TestSchema("Orders", parentProps, CreateLayout(),
            children: new List<ISchemaDefinition<TestProperty>> { childSchema });

        parentSchema.Children.ShouldNotBeNull();
        parentSchema.Children!.Count.ShouldBe(1);
        parentSchema.Children[0].Name.ShouldBe("OrderDetails");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void SchemaWithIndexes()
    {
        var props = new List<TestProperty>
        {
            CreateProperty("Id", "Surrogate"),
            CreateProperty("Name")
        };

        var index = new IndexDefinition<TestProperty>(
            "IX_Test_Name",
            [new IndexMember(0, "Name")]);

        var schema = new TestSchema("Test", props, CreateLayout(),
            indexes: new List<IIndexDefinition<TestProperty>> { index });

        schema.Indexes.Count.ShouldBe(1);
        schema.Indexes[0].Name.ShouldBe("IX_Test_Name");
    }
}
