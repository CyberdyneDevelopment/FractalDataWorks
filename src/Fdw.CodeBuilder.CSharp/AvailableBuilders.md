# Fdw.CodeBuilder - Available Builders Reference

This document provides a comprehensive reference of all builders, methods, and overloads available in the Fdw.CodeBuilder library.

## Overview

The CodeBuilder library provides a fluent API for generating C# code through builder patterns. All builders implement the base `ICodeBuilder` interface and support method chaining.

## Base Interfaces and Classes

### ICodeBuilder (Base Interface)
The foundation interface for all code builders.

**Properties:**
- `int IndentLevel { get; }` - Gets the current indentation level
- `string IndentString { get; set; }` - Gets or sets the indentation string (default: 4 spaces)

**Methods:**
- `string Build()` - Builds the code and returns it as a string

### CodeBuilderBase (Abstract Base Class)
Abstract base class providing common functionality for all builders.

**Protected Methods:**
- `void AppendLine(string line)` - Appends a line with proper indentation
- `void Append(string text)` - Appends text without a newline
- `void Indent()` - Increases the indentation level
- `void Outdent()` - Decreases the indentation level
- `void Clear()` - Clears the builder and resets indentation

## Builder Classes

### 1. ClassBuilder

**Purpose:** Generates complete C# class definitions with all members and modifiers.

#### Configuration Methods

| Method | Parameters | Return Type | Description |
|--------|------------|-------------|-------------|
| `WithNamespace` | `string namespaceName` | `IClassBuilder` | Sets the namespace for the class |
| `WithUsings` | `params string[] usings` | `IClassBuilder` | Adds using directives |
| `WithName` | `string className` | `IClassBuilder` | Sets the class name |
| `WithAccessModifier` | `string accessModifier` | `IClassBuilder` | Sets access modifier (public, internal, etc.) |
| `AsStatic` | - | `IClassBuilder` | Makes the class static |
| `AsAbstract` | - | `IClassBuilder` | Makes the class abstract |
| `AsSealed` | - | `IClassBuilder` | Makes the class sealed |
| `AsPartial` | - | `IClassBuilder` | Makes the class partial |

#### Inheritance Methods

| Method | Parameters | Return Type | Description |
|--------|------------|-------------|-------------|
| `WithBaseClass` | `string baseClass` | `IClassBuilder` | Sets the base class |
| `WithInterfaces` | `params string[] interfaces` | `IClassBuilder` | Adds implemented interfaces |

#### Generic Methods

| Method | Parameters | Return Type | Description |
|--------|------------|-------------|-------------|
| `WithGenericParameters` | `params string[] typeParameters` | `IClassBuilder` | Adds generic type parameters |
| `WithGenericConstraint` | `string typeParameter, params string[] constraints` | `IClassBuilder` | Adds generic type constraints |

#### Member Methods

| Method | Parameters | Return Type | Description |
|--------|------------|-------------|-------------|
| `WithField` | `IFieldBuilder fieldBuilder` | `IClassBuilder` | Adds a field to the class |
| `WithProperty` | `IPropertyBuilder propertyBuilder` | `IClassBuilder` | Adds a property to the class |
| `WithMethod` | `IMethodBuilder methodBuilder` | `IClassBuilder` | Adds a method to the class |
| `WithConstructor` | `IConstructorBuilder constructorBuilder` | `IClassBuilder` | Adds a constructor to the class |
| `WithNestedClass` | `IClassBuilder nestedClassBuilder` | `IClassBuilder` | Adds a nested class |

#### Documentation Methods

| Method | Parameters | Return Type | Description |
|--------|------------|-------------|-------------|
| `WithAttribute` | `string attribute` | `IClassBuilder` | Adds an attribute to the class |
| `WithXmlDoc` | `string summary` | `IClassBuilder` | Adds XML documentation comments |

### 2. MethodBuilder

**Purpose:** Generates C# method definitions with full signature and body support.

#### Basic Configuration

| Method | Parameters | Return Type | Description |
|--------|------------|-------------|-------------|
| `WithName` | `string name` | `IMethodBuilder` | Sets the method name |
| `WithReturnType` | `string returnType` | `IMethodBuilder` | Sets the return type |
| `WithAccessModifier` | `string accessModifier` | `IMethodBuilder` | Sets the access modifier |

#### Method Modifiers

| Method | Parameters | Return Type | Description |
|--------|------------|-------------|-------------|
| `AsStatic` | - | `IMethodBuilder` | Makes the method static |
| `AsVirtual` | - | `IMethodBuilder` | Makes the method virtual |
| `AsOverride` | - | `IMethodBuilder` | Makes the method override |
| `AsAbstract` | - | `IMethodBuilder` | Makes the method abstract |
| `AsAsync` | - | `IMethodBuilder` | Makes the method async |

#### Parameters and Generics

| Method | Parameters | Return Type | Description |
|--------|------------|-------------|-------------|
| `WithParameter` | `string type, string name, string? defaultValue = null` | `IMethodBuilder` | Adds a parameter |
| `WithGenericParameters` | `params string[] typeParameters` | `IMethodBuilder` | Adds generic type parameters |
| `WithGenericConstraint` | `string typeParameter, params string[] constraints` | `IMethodBuilder` | Adds generic constraints |

#### Body and Implementation

| Method | Parameters | Return Type | Description |
|--------|------------|-------------|-------------|
| `WithBody` | `string body` | `IMethodBuilder` | Sets the method body |
| `AddBodyLine` | `string line` | `IMethodBuilder` | Adds a line to the method body |
| `WithExpressionBody` | `string expression` | `IMethodBuilder` | Sets as expression body method |

#### Documentation

| Method | Parameters | Return Type | Description |
|--------|------------|-------------|-------------|
| `WithAttribute` | `string attribute` | `IMethodBuilder` | Adds an attribute |
| `WithXmlDoc` | `string summary` | `IMethodBuilder` | Adds XML documentation |
| `WithParamDoc` | `string parameterName, string description` | `IMethodBuilder` | Adds parameter documentation |
| `WithReturnDoc` | `string description` | `IMethodBuilder` | Adds return value documentation |

### 3. PropertyBuilder

**Purpose:** Generates C# property definitions with accessors and modifiers.

#### Basic Configuration

| Method | Parameters | Return Type | Description |
|--------|------------|-------------|-------------|
| `WithName` | `string name` | `IPropertyBuilder` | Sets the property name |
| `WithType` | `string type` | `IPropertyBuilder` | Sets the property type |
| `WithAccessModifier` | `string accessModifier` | `IPropertyBuilder` | Sets the access modifier |

#### Property Modifiers

| Method | Parameters | Return Type | Description |
|--------|------------|-------------|-------------|
| `AsStatic` | - | `IPropertyBuilder` | Makes the property static |
| `AsVirtual` | - | `IPropertyBuilder` | Makes the property virtual |
| `AsOverride` | - | `IPropertyBuilder` | Makes the property override |
| `AsAbstract` | - | `IPropertyBuilder` | Makes the property abstract |

#### Accessor Configuration

| Method | Parameters | Return Type | Description |
|--------|------------|-------------|-------------|
| `AsReadOnly` | - | `IPropertyBuilder` | Makes property read-only (get only) |
| `AsWriteOnly` | - | `IPropertyBuilder` | Makes property write-only (set only) |
| `WithGetter` | `string getterBody` | `IPropertyBuilder` | Sets custom getter implementation |
| `WithSetter` | `string setterBody` | `IPropertyBuilder` | Sets custom setter implementation |
| `WithGetterAccessModifier` | `string accessModifier` | `IPropertyBuilder` | Sets getter access modifier |
| `WithSetterAccessModifier` | `string accessModifier` | `IPropertyBuilder` | Sets setter access modifier |
| `WithInitSetter` | - | `IPropertyBuilder` | Makes property use init-only setter |

#### Value and Expression

| Method | Parameters | Return Type | Description |
|--------|------------|-------------|-------------|
| `WithInitializer` | `string initializer` | `IPropertyBuilder` | Sets an initializer value |
| `WithExpressionBody` | `string expression` | `IPropertyBuilder` | Sets as expression body property |

#### Documentation

| Method | Parameters | Return Type | Description |
|--------|------------|-------------|-------------|
| `WithAttribute` | `string attribute` | `IPropertyBuilder` | Adds an attribute |
| `WithXmlDoc` | `string summary` | `IPropertyBuilder` | Adds XML documentation |

### 4. FieldBuilder

**Purpose:** Generates C# field definitions with modifiers and initializers.

#### Basic Configuration

| Method | Parameters | Return Type | Description |
|--------|------------|-------------|-------------|
| `WithName` | `string name` | `IFieldBuilder` | Sets the field name |
| `WithType` | `string type` | `IFieldBuilder` | Sets the field type |
| `WithAccessModifier` | `string accessModifier` | `IFieldBuilder` | Sets the access modifier |

#### Field Modifiers

| Method | Parameters | Return Type | Description |
|--------|------------|-------------|-------------|
| `AsStatic` | - | `IFieldBuilder` | Makes the field static |
| `AsReadOnly` | - | `IFieldBuilder` | Makes the field readonly |
| `AsConst` | - | `IFieldBuilder` | Makes the field const |
| `AsVolatile` | - | `IFieldBuilder` | Makes the field volatile |

#### Value and Documentation

| Method | Parameters | Return Type | Description |
|--------|------------|-------------|-------------|
| `WithInitializer` | `string initializer` | `IFieldBuilder` | Sets an initializer expression |
| `WithAttribute` | `string attribute` | `IFieldBuilder` | Adds an attribute |
| `WithXmlDoc` | `string summary` | `IFieldBuilder` | Adds XML documentation |

### 5. ConstructorBuilder

**Purpose:** Generates C# constructor definitions with parameters and body.

#### Basic Configuration

| Method | Parameters | Return Type | Description |
|--------|------------|-------------|-------------|
| `WithClassName` | `string className` | `ConstructorBuilder` | Sets the class name for the constructor |
| `WithAccessModifier` | `string accessModifier` | `IConstructorBuilder` | Sets the access modifier |
| `AsStatic` | - | `IConstructorBuilder` | Makes the constructor static |

#### Parameters and Chaining

| Method | Parameters | Return Type | Description |
|--------|------------|-------------|-------------|
| `WithParameter` | `string type, string name, string? defaultValue = null` | `IConstructorBuilder` | Adds a parameter |
| `WithBaseCall` | `params string[] arguments` | `IConstructorBuilder` | Adds a base constructor call |
| `WithThisCall` | `params string[] arguments` | `IConstructorBuilder` | Adds a this constructor call |

#### Body and Implementation

| Method | Parameters | Return Type | Description |
|--------|------------|-------------|-------------|
| `WithBody` | `string body` | `IConstructorBuilder` | Sets the constructor body |
| `AddBodyLine` | `string line` | `IConstructorBuilder` | Adds a line to the constructor body |

#### Documentation

| Method | Parameters | Return Type | Description |
|--------|------------|-------------|-------------|
| `WithAttribute` | `string attribute` | `IConstructorBuilder` | Adds an attribute |
| `WithXmlDoc` | `string summary` | `IConstructorBuilder` | Adds XML documentation |
| `WithParamDoc` | `string parameterName, string description` | `IConstructorBuilder` | Adds parameter documentation |

### 6. InterfaceBuilder

**Purpose:** Generates C# interface definitions with members and constraints.

#### Basic Configuration

| Method | Parameters | Return Type | Description |
|--------|------------|-------------|-------------|
| `WithNamespace` | `string namespaceName` | `IInterfaceBuilder` | Sets the namespace |
| `WithUsings` | `params string[] usings` | `IInterfaceBuilder` | Adds using directives |
| `WithName` | `string interfaceName` | `IInterfaceBuilder` | Sets the interface name |
| `WithAccessModifier` | `string accessModifier` | `IInterfaceBuilder` | Sets access modifier |
| `AsPartial` | - | `IInterfaceBuilder` | Makes the interface partial |

#### Inheritance and Generics

| Method | Parameters | Return Type | Description |
|--------|------------|-------------|-------------|
| `WithBaseInterfaces` | `params string[] interfaces` | `IInterfaceBuilder` | Adds base interfaces |
| `WithGenericParameters` | `params string[] typeParameters` | `IInterfaceBuilder` | Adds generic type parameters |
| `WithGenericConstraint` | `string typeParameter, params string[] constraints` | `IInterfaceBuilder` | Adds generic constraints |

#### Members

| Method | Parameters | Return Type | Description |
|--------|------------|-------------|-------------|
| `WithProperty` | `IPropertyBuilder propertyBuilder` | `IInterfaceBuilder` | Adds a property |
| `WithMethod` | `IMethodBuilder methodBuilder` | `IInterfaceBuilder` | Adds a method |
| `WithEvent` | `string eventType, string eventName` | `IInterfaceBuilder` | Adds an event |

#### Documentation

| Method | Parameters | Return Type | Description |
|--------|------------|-------------|-------------|
| `WithAttribute` | `string attribute` | `IInterfaceBuilder` | Adds an attribute |
| `WithXmlDoc` | `string summary` | `IInterfaceBuilder` | Adds XML documentation |

### 7. EnumBuilder

**Purpose:** Generates C# enum definitions with members and underlying types.

#### Basic Configuration

| Method | Parameters | Return Type | Description |
|--------|------------|-------------|-------------|
| `WithNamespace` | `string namespaceName` | `IEnumBuilder` | Sets the namespace |
| `WithUsings` | `params string[] usings` | `IEnumBuilder` | Adds using directives |
| `WithName` | `string enumName` | `IEnumBuilder` | Sets the enum name |
| `WithAccessModifier` | `string accessModifier` | `IEnumBuilder` | Sets access modifier |
| `WithUnderlyingType` | `string underlyingType` | `IEnumBuilder` | Sets underlying type (byte, int, long, etc.) |

#### Enum Members

| Method | Parameters | Return Type | Description |
|--------|------------|-------------|-------------|
| `WithMember` | `string name, int? value = null` | `IEnumBuilder` | Adds an enum member with optional value |
| `WithMember` | `string name, string value` | `IEnumBuilder` | Adds an enum member with string value |
| `WithMemberAttribute` | `string memberName, string attribute` | `IEnumBuilder` | Adds attribute to specific member |
| `WithMemberXmlDoc` | `string memberName, string summary` | `IEnumBuilder` | Adds XML doc to specific member |

#### Documentation

| Method | Parameters | Return Type | Description |
|--------|------------|-------------|-------------|
| `WithAttribute` | `string attribute` | `IEnumBuilder` | Adds an attribute |
| `WithXmlDoc` | `string summary` | `IEnumBuilder` | Adds XML documentation |

## Code Generation and Language Support

### CSharpCodeGenerator

**Purpose:** Transforms builders and syntax trees into C# source code.

**Properties:**
- `string TargetLanguage` - Returns "csharp"

**Methods:**

| Method | Parameters | Return Type | Description |
|--------|------------|-------------|-------------|
| `Generate` | `ISyntaxTree syntaxTree` | `string` | Generates code from syntax tree |
| `Generate` | `IClassBuilder classBuilder` | `string` | Generates code from class builder |
| `Generate` | `IInterfaceBuilder interfaceBuilder` | `string` | Generates code from interface builder |
| `Generate` | `IEnumBuilder enumBuilder` | `string` | Generates code from enum builder |
| `GenerateCompilationUnit` | `IEnumerable<ICodeBuilder> builders` | `string` | Generates compilation unit with multiple types |

### LanguageRegistry

**Purpose:** Manages language-specific parsers and code generation.

**Properties:**
- `IReadOnlyList<string> SupportedLanguages` - Gets supported languages

**Methods:**

| Method | Parameters | Return Type | Description |
|--------|------------|-------------|-------------|
| `IsSupported` | `string language` | `bool` | Checks if language is supported |
| `GetExtensions` | `string language` | `IReadOnlyList<string>` | Gets file extensions for language |
| `GetLanguageByExtension` | `string extension` | `string?` | Gets language by file extension |
| `GetParser` | `string language, CancellationToken cancellationToken = default` | `Task<ICodeParser?>` | Gets parser for language |
| `RegisterParser` | `string language, ICodeParser parser, params string[] extensions` | `void` | Registers parser for language |

## Parsing Support

### RoslynCSharpParser

**Purpose:** Roslyn-based parser for C# source code analysis.

**Properties:**
- `string Language` - Returns "csharp"

**Methods:**

| Method | Parameters | Return Type | Description |
|--------|------------|-------------|-------------|
| `ParseAsync` | `string sourceCode, string? filePath = null, CancellationToken cancellationToken = default` | `Task<IGenericResult<ISyntaxTree>>` | Parses source code into syntax tree |
| `ValidateAsync` | `string sourceCode, CancellationToken cancellationToken = default` | `Task<IGenericResult>` | Validates source code syntax |

### Syntax Tree Interfaces

#### ISyntaxTree
**Properties:**
- `ISyntaxNode Root` - Gets the root node
- `string SourceText` - Gets the source text
- `string Language` - Gets the language
- `string? FilePath` - Gets the file path
- `bool HasErrors` - Gets whether tree contains errors

**Methods:**
- `IEnumerable<ISyntaxNode> GetErrors()` - Gets all error nodes
- `IEnumerable<ISyntaxNode> FindNodes(string nodeType)` - Finds nodes by type
- `ISyntaxNode? GetNodeAtPosition(int position)` - Gets node at position
- `ISyntaxNode? GetNodeAtLocation(int line, int column)` - Gets node at line/column

#### ISyntaxNode
**Properties:**
- `string NodeType` - Gets the node type
- `string Text` - Gets the text content
- `int StartPosition/EndPosition` - Gets positions
- `int StartLine/StartColumn` - Gets line/column
- `IReadOnlyList<ISyntaxNode> Children` - Gets child nodes
- `ISyntaxNode? Parent` - Gets parent node
- `bool IsTerminal/IsError` - Gets node state

**Methods:**
- `ISyntaxNode? FindChild(string nodeType)` - Finds first child of type
- `IEnumerable<ISyntaxNode> FindChildren(string nodeType)` - Finds all children of type
- `IEnumerable<ISyntaxNode> DescendantNodes()` - Gets all descendant nodes

## Usage Examples

### Basic Class Generation
```csharp
var classCode = new ClassBuilder()
    .WithNamespace("MyNamespace")
    .WithUsings("System", "System.Collections.Generic")
    .WithName("MyClass")
    .WithAccessModifier("public")
    .AsPartial()
    .WithProperty(new PropertyBuilder()
        .WithName("Id")
        .WithType("int")
        .AsReadOnly())
    .WithMethod(new MethodBuilder()
        .WithName("GetValue")
        .WithReturnType("string")
        .WithExpressionBody("Id.ToString()"))
    .Build();
```

### Advanced Method with Generics
```csharp
var methodCode = new MethodBuilder()
    .WithName("Process")
    .WithReturnType("Task<T>")
    .WithGenericParameters("T")
    .WithGenericConstraint("T", "class", "new()")
    .WithParameter("IEnumerable<T>", "items")
    .WithParameter("CancellationToken", "cancellationToken", "default")
    .AsAsync()
    .WithBody("// Implementation here")
    .WithXmlDoc("Processes a collection of items asynchronously")
    .WithParamDoc("items", "The items to process")
    .WithReturnDoc("The processed result")
    .Build();
```

This comprehensive reference covers all available builders, methods, and overloads in the Fdw.CodeBuilder library, providing developers with a complete guide for programmatic C# code generation.