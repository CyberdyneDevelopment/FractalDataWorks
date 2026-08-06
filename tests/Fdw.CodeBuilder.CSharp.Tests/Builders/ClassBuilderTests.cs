using Fdw.CodeBuilder.CSharp.Builders;

namespace Fdw.CodeBuilder.CSharp.Tests.Builders;

public class ClassBuilderTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void Build_DefaultClass_GeneratesBasicClass()
    {
        // Arrange
        var builder = new ClassBuilder();

        // Act
        var result = builder.Build();

        // Assert
        result.ShouldContain("public class MyClass");
        result.ShouldContain("{");
        result.ShouldContain("}");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void WithName_SetsClassName()
    {
        // Arrange
        var builder = new ClassBuilder();

        // Act
        var result = builder.WithName("Person").Build();

        // Assert
        result.ShouldContain("class Person");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void WithNamespace_AddsNamespace()
    {
        // Arrange
        var builder = new ClassBuilder();

        // Act
        var result = builder.WithNamespace("MyApp.Models").Build();

        // Assert
        result.ShouldContain("namespace MyApp.Models;");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void WithUsings_AddsUsingDirectives()
    {
        // Arrange
        var builder = new ClassBuilder();

        // Act
        var result = builder.WithUsings("System", "System.Collections.Generic").Build();

        // Assert
        result.ShouldContain("using System;");
        result.ShouldContain("using System.Collections.Generic;");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void WithUsings_DuplicateUsings_AddsOnlyOnce()
    {
        // Arrange
        var builder = new ClassBuilder();

        // Act
        var result = builder.WithUsings("System", "System", "System").Build();

        // Assert
        var count = result.Split("using System;").Length - 1;
        count.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void WithAccessModifier_SetsModifier()
    {
        // Arrange
        var builder = new ClassBuilder();

        // Act
        var result = builder.WithAccessModifier("internal").Build();

        // Assert
        result.ShouldContain("internal class MyClass");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void AsStatic_AddsStaticKeyword()
    {
        // Arrange
        var builder = new ClassBuilder();

        // Act
        var result = builder.AsStatic().Build();

        // Assert
        result.ShouldContain("public static class MyClass");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void AsAbstract_AddsAbstractKeyword()
    {
        // Arrange
        var builder = new ClassBuilder();

        // Act
        var result = builder.AsAbstract().Build();

        // Assert
        result.ShouldContain("public abstract class MyClass");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void AsSealed_AddsSealedKeyword()
    {
        // Arrange
        var builder = new ClassBuilder();

        // Act
        var result = builder.AsSealed().Build();

        // Assert
        result.ShouldContain("public sealed class MyClass");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void AsAbstract_ClearsSealed()
    {
        // Arrange
        var builder = new ClassBuilder();

        // Act
        var result = builder.AsSealed().AsAbstract().Build();

        // Assert
        result.ShouldContain("abstract");
        result.ShouldNotContain("sealed");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void AsPartial_AddsPartialKeyword()
    {
        // Arrange
        var builder = new ClassBuilder();

        // Act
        var result = builder.AsPartial().Build();

        // Assert
        result.ShouldContain("public partial class MyClass");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void WithBaseClass_AddsInheritance()
    {
        // Arrange
        var builder = new ClassBuilder();

        // Act
        var result = builder.WithBaseClass("BaseClass").Build();

        // Assert
        result.ShouldContain("class MyClass : BaseClass");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void WithInterfaces_AddsInterfaces()
    {
        // Arrange
        var builder = new ClassBuilder();

        // Act
        var result = builder.WithInterfaces("IDisposable", "IEquatable<MyClass>").Build();

        // Assert
        result.ShouldContain(": IDisposable, IEquatable<MyClass>");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void WithBaseClassAndInterfaces_CombinesCorrectly()
    {
        // Arrange
        var builder = new ClassBuilder();

        // Act
        var result = builder
            .WithBaseClass("BaseClass")
            .WithInterfaces("IDisposable")
            .Build();

        // Assert
        result.ShouldContain(": BaseClass, IDisposable");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void WithGenericParameters_AddsTypeParameters()
    {
        // Arrange
        var builder = new ClassBuilder();

        // Act
        var result = builder.WithGenericParameters("T").Build();

        // Assert
        result.ShouldContain("class MyClass<T>");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void WithGenericParameters_Multiple_AddsAll()
    {
        // Arrange
        var builder = new ClassBuilder();

        // Act
        var result = builder.WithGenericParameters("T", "U").Build();

        // Assert
        result.ShouldContain("class MyClass<T, U>");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void WithGenericConstraint_AddsConstraint()
    {
        // Arrange
        var builder = new ClassBuilder();

        // Act
        var result = builder
            .WithGenericParameters("T")
            .WithGenericConstraint("T", "class")
            .Build();

        // Assert
        result.ShouldContain("where T : class");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void WithAttribute_AddsAttribute()
    {
        // Arrange
        var builder = new ClassBuilder();

        // Act
        var result = builder.WithAttribute("Serializable").Build();

        // Assert
        result.ShouldContain("[Serializable]");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void WithXmlDoc_GeneratesXmlDocumentation()
    {
        // Arrange
        var builder = new ClassBuilder();

        // Act
        var result = builder.WithXmlDoc("Represents a person.").Build();

        // Assert
        result.ShouldContain("/// <summary>");
        result.ShouldContain("/// Represents a person.");
        result.ShouldContain("/// </summary>");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void WithField_AddsField()
    {
        // Arrange
        var builder = new ClassBuilder();
        var field = new FieldBuilder().WithName("_name").WithType("string");

        // Act
        var result = builder.WithField(field).Build();

        // Assert
        result.ShouldContain("private string _name;");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void WithProperty_AddsProperty()
    {
        // Arrange
        var builder = new ClassBuilder();
        var property = new PropertyBuilder().WithName("Name").WithType("string");

        // Act
        var result = builder.WithProperty(property).Build();

        // Assert
        result.ShouldContain("public string Name");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void WithMethod_AddsMethod()
    {
        // Arrange
        var builder = new ClassBuilder();
        var method = new MethodBuilder().WithName("Execute").WithReturnType("void");

        // Act
        var result = builder.WithMethod(method).Build();

        // Assert
        result.ShouldContain("public void Execute()");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void WithConstructor_AddsConstructor()
    {
        // Arrange
        var builder = new ClassBuilder();
        var constructor = new ConstructorBuilder().WithClassName("MyClass");

        // Act
        var result = builder.WithConstructor(constructor).Build();

        // Assert
        result.ShouldContain("public MyClass()");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void WithNestedClass_AddsNestedClass()
    {
        // Arrange
        var builder = new ClassBuilder();
        var nested = new ClassBuilder().WithName("NestedClass");

        // Act
        var result = builder.WithNestedClass(nested).Build();

        // Assert
        result.ShouldContain("public class NestedClass");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void Build_FullClass_GeneratesCompleteCode()
    {
        // Arrange
        var builder = new ClassBuilder();
        var field = new FieldBuilder().WithName("_name").WithType("string").AsReadOnly();
        var property = new PropertyBuilder().WithName("Name").WithType("string").AsReadOnly();
        var constructor = new ConstructorBuilder()
            .WithClassName("Person")
            .WithParameter("string", "name")
            .AddBodyLine("_name = name;");

        // Act
        var result = builder
            .WithNamespace("MyApp.Models")
            .WithUsings("System")
            .WithName("Person")
            .WithXmlDoc("Represents a person.")
            .WithField(field)
            .WithProperty(property)
            .WithConstructor(constructor)
            .Build();

        // Assert
        result.ShouldContain("namespace MyApp.Models;");
        result.ShouldContain("using System;");
        result.ShouldContain("/// <summary>");
        result.ShouldContain("public class Person");
        result.ShouldContain("private readonly string _name;");
        result.ShouldContain("public string Name");
        result.ShouldContain("public Person(string name)");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void Build_EmptyClass_GeneratesEmptyBlock()
    {
        // Arrange
        var builder = new ClassBuilder();

        // Act
        var result = builder.Build();

        // Assert
        result.ShouldContain("{");
        result.ShouldContain("}");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void Build_WithAllMemberTypes_OrdersCorrectly()
    {
        // Arrange
        var builder = new ClassBuilder();
        var field = new FieldBuilder().WithName("_field");
        var constructor = new ConstructorBuilder().WithClassName("MyClass");
        var property = new PropertyBuilder().WithName("Property");
        var method = new MethodBuilder().WithName("Method");

        // Act
        var result = builder
            .WithField(field)
            .WithConstructor(constructor)
            .WithProperty(property)
            .WithMethod(method)
            .Build();

        // Assert
        var fieldIndex = result.IndexOf("_field");
        var constructorIndex = result.IndexOf("MyClass()");
        var propertyIndex = result.IndexOf("Property");
        var methodIndex = result.IndexOf("Method()");

        fieldIndex.ShouldBeLessThan(constructorIndex);
        constructorIndex.ShouldBeLessThan(propertyIndex);
        propertyIndex.ShouldBeLessThan(methodIndex);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void Build_WithMultipleFields_SeparatesFromOtherMembers()
    {
        // Arrange
        var builder = new ClassBuilder();
        var field1 = new FieldBuilder().WithName("_field1");
        var field2 = new FieldBuilder().WithName("_field2");
        var property = new PropertyBuilder().WithName("Property");

        // Act
        var result = builder
            .WithField(field1)
            .WithField(field2)
            .WithProperty(property)
            .Build();

        // Assert
        result.ShouldContain("_field1");
        result.ShouldContain("_field2");
        result.ShouldContain("Property");
    }
}
