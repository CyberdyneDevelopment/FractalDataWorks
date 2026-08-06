using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Fdw.CodeBuilder.Analysis.CSharp.Generators;

/// <summary>
/// Generator that creates Equals/GetHashCode implementations for marked classes.
/// </summary>
[Generator]
public class EqualsGenerator : IIncrementalGenerator
{
    private readonly string _attributeSource;
    private readonly string _attributeName;

    /// <summary>
    /// Initializes a new instance of the <see cref="EqualsGenerator"/> class.
    /// </summary>
    /// <param name="attributeSource">The source code for the GenerateEquals attribute.</param>
    /// <param name="attributeName">The name of the attribute to look for.</param>
    public EqualsGenerator(string attributeSource, string attributeName = "GenerateEquals")
    {
        _attributeSource = attributeSource;
        _attributeName = attributeName;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EqualsGenerator"/> class with default attribute source.
    /// </summary>
    public EqualsGenerator()
        : this(DefaultAttributeSource)
    {
    }

    /// <summary>
    /// Gets the default source for the GenerateEquals attribute.
    /// </summary>
    public static string DefaultAttributeSource => @"
using System;

namespace TestNamespace
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class GenerateEqualsAttribute : Attribute
    {
    }
}";

    /// <inheritdoc/>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        RegisterAttributeSource(context);
        ProcessEqualsClasses(context);
    }

    private void RegisterAttributeSource(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(ctx =>
        {
            ctx.AddSource("GenerateEqualsAttribute.g.cs", _attributeSource);
        });
    }

    private void ProcessEqualsClasses(IncrementalGeneratorInitializationContext context)
    {
        var classDeclarations = CreateEqualsClassProvider(context);
        context.RegisterSourceOutput(classDeclarations, (ctx, classNode) => GenerateEqualsCode(ctx, classNode));
    }

    private IncrementalValuesProvider<ClassDeclarationSyntax> CreateEqualsClassProvider(IncrementalGeneratorInitializationContext context)
    {
        return context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: (node, _) => node is ClassDeclarationSyntax,
                transform: (ctx, _) => FilterClassWithAttribute((ClassDeclarationSyntax)ctx.Node))
            .Where(node => node != null)!;
    }

    private ClassDeclarationSyntax? FilterClassWithAttribute(ClassDeclarationSyntax classNode)
    {
        return HasEqualsAttribute(classNode) ? classNode : null;
    }

    private bool HasEqualsAttribute(ClassDeclarationSyntax classNode)
    {
        foreach (var attrList in classNode.AttributeLists)
        {
            foreach (var attr in attrList.Attributes)
            {
                if (attr.Name.ToString().Contains(_attributeName))
                {
                    return true;
                }
            }
        }
        return false;
    }

    private static void GenerateEqualsCode(SourceProductionContext ctx, ClassDeclarationSyntax classNode)
    {
        if (classNode == null) return;

        var className = classNode.Identifier.Text;
        var namespaceName = GetContainingNamespace(classNode);
        var properties = ExtractProperties(classNode);
        var code = GenerateEqualsImplementation(className, namespaceName, properties);
        ctx.AddSource($"{className}.Equals.g.cs", code);
    }

    private static List<string> ExtractProperties(ClassDeclarationSyntax classNode)
    {
        var properties = new List<string>();
        foreach (var member in classNode.Members)
        {
            if (member is PropertyDeclarationSyntax property && HasGetter(property))
            {
                properties.Add(property.Identifier.Text);
            }
        }
        return properties;
    }

    private static bool HasGetter(PropertyDeclarationSyntax property)
    {
        return property.AccessorList?.Accessors
            .Any(accessor => accessor.IsKind(SyntaxKind.GetAccessorDeclaration)) == true;
    }

    private static string GetContainingNamespace(ClassDeclarationSyntax classNode)
    {
        // Find the namespace declaration
        var parent = classNode.Parent;
        while (parent != null &&
               parent is not NamespaceDeclarationSyntax &&
               parent is not FileScopedNamespaceDeclarationSyntax)
        {
            parent = parent.Parent;
        }

        if (parent is NamespaceDeclarationSyntax nsDecl)
        {
            return nsDecl.Name.ToString();
        }
        else if (parent is FileScopedNamespaceDeclarationSyntax fsNsDecl)
        {
            return fsNsDecl.Name.ToString();
        }

        return "DefaultNamespace";
    }

    private static string GenerateEqualsImplementation(string className, string namespaceName, List<string> properties)
    {
        var header = GenerateHeader(namespaceName, className);
        var equalsMethod = GenerateEqualsMethod(className);
        var equalsTypedMethod = GenerateEqualsTypedMethod(className, properties);
        var hashCodeMethod = GenerateHashCodeMethod(properties);
        var footer = GenerateFooter();

        return header + equalsMethod + equalsTypedMethod + hashCodeMethod + footer;
    }

    private static string GenerateHeader(string namespaceName, string className)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine();
        sb.AppendLine("using System;");
        sb.AppendLine();
        sb.AppendLine($"namespace {namespaceName}");
        sb.AppendLine("{");
        sb.AppendLine($"    public partial class {className}");
        sb.AppendLine("    {");
        return sb.ToString();
    }

    private static string GenerateEqualsMethod(string className)
    {
        var sb = new StringBuilder();
        sb.AppendLine("        public override bool Equals(object obj)");
        sb.AppendLine("        {");
        sb.AppendLine($"            return obj is {className} other && Equals(other);");
        sb.AppendLine("        }");
        sb.AppendLine();
        return sb.ToString();
    }

    private static string GenerateEqualsTypedMethod(string className, List<string> properties)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"        public bool Equals({className} other)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (other is null) return false;");
        sb.AppendLine("            if (ReferenceEquals(this, other)) return true;");
        sb.AppendLine();

        if (properties.Count > 0)
        {
            sb.AppendLine("            return ");
            for (var i = 0; i < properties.Count; i++)
            {
                var prop = properties[i];
                if (i > 0) sb.AppendLine(" && ");
                sb.Append($"                EqualityComparer<object>.Default.Equals({prop}, other.{prop})");
            }
            sb.AppendLine(";");
        }
        else
        {
            sb.AppendLine("            return true;");
        }

        sb.AppendLine("        }");
        sb.AppendLine();
        return sb.ToString();
    }

    private static string GenerateHashCodeMethod(List<string> properties)
    {
        var sb = new StringBuilder();
        sb.AppendLine("        public override int GetHashCode()");
        sb.AppendLine("        {");

        if (properties.Count > 0)
        {
            sb.AppendLine("            var hashCode = new HashCode();");
            foreach (var prop in properties)
            {
                sb.AppendLine($"            hashCode.Add({prop});");
            }
            sb.AppendLine("            return hashCode.ToHashCode();");
        }
        else
        {
            sb.AppendLine("            return base.GetHashCode();");
        }

        sb.AppendLine("        }");
        return sb.ToString();
    }

    private static string GenerateFooter()
    {
        var sb = new StringBuilder();
        sb.AppendLine("    }");
        sb.AppendLine("}");
        return sb.ToString();
    }
}