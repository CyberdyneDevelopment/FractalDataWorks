using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Fdw.CodeBuilder.Analysis.CSharp.Generators;

/// <summary>
/// Test source generator that reports diagnostics.
/// </summary>
[Generator]
public class DiagnosticGenerator : IIncrementalGenerator
{
    private readonly DiagnosticDescriptor _missingAttributeError;
    private readonly string _attributeName;

    /// <summary>
    /// Initializes a new instance of the <see cref="DiagnosticGenerator"/> class.
    /// </summary>
    /// <param name="diagnosticId">The diagnostic ID.</param>
    /// <param name="title">The diagnostic title.</param>
    /// <param name="messageFormat">The diagnostic message format.</param>
    /// <param name="category">The diagnostic category.</param>
    /// <param name="attributeName">The name of the attribute to check for.</param>
    public DiagnosticGenerator(
        string diagnosticId = "TEST001",
        string title = "Missing Required Attribute",
        string messageFormat = "Class {0} is missing the required attribute",
        string category = "TestGenerator",
        string attributeName = "GenerateCode")
    {
        _missingAttributeError = new DiagnosticDescriptor(
            id: diagnosticId,
            title: title,
            messageFormat: messageFormat,
            category: category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);
        _attributeName = attributeName;
    }

    /// <inheritdoc/>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        ProcessClasses(context);
    }

    private void ProcessClasses(IncrementalGeneratorInitializationContext context)
    {
        var classDeclarations = CreateClassSyntaxProvider(context);
        RegisterDiagnosticOutput(context, classDeclarations);
        RegisterInterfaceOutput(context, classDeclarations);
    }

    private static IncrementalValuesProvider<ClassDeclarationSyntax> CreateClassSyntaxProvider(IncrementalGeneratorInitializationContext context)
    {
        return context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: (node, _) => node is ClassDeclarationSyntax,
                transform: (ctx, _) => (ClassDeclarationSyntax)ctx.Node)
            .Where(node => node != null);
    }

    private void RegisterDiagnosticOutput(IncrementalGeneratorInitializationContext context, IncrementalValuesProvider<ClassDeclarationSyntax> classDeclarations)
    {
        context.RegisterSourceOutput(classDeclarations, (ctx, classNode) => ProcessDiagnostic(ctx, classNode));
    }

    private void ProcessDiagnostic(SourceProductionContext ctx, ClassDeclarationSyntax classNode)
    {
        if (classNode == null) return;

        var className = classNode.Identifier.Text;
        if (!HasAttribute(classNode, _attributeName))
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                _missingAttributeError,
                classNode.GetLocation(),
                className));
        }
    }

    private static void RegisterInterfaceOutput(IncrementalGeneratorInitializationContext context, IncrementalValuesProvider<ClassDeclarationSyntax> classDeclarations)
    {
        context.RegisterSourceOutput(classDeclarations, (ctx, classNode) => ProcessInterface(ctx, classNode));
    }

    private static void ProcessInterface(SourceProductionContext ctx, ClassDeclarationSyntax classNode)
    {
        if (classNode == null) return;

        var className = classNode.Identifier.Text;
        var namespaceName = GetContainingNamespace(classNode);

        var source = $$"""

                       namespace {{namespaceName}};

                       public interface I{{className}}
                       {
                           void DoSomething();
                       }
                       """;

        ctx.AddSource($"I{className}.g.cs", source);
    }

    private static bool HasAttribute(ClassDeclarationSyntax classNode, string attributeName)
    {
        foreach (var attrList in classNode.AttributeLists)
        {
            foreach (var attr in attrList.Attributes)
            {
                if (attr.Name.ToString().Contains(attributeName))
                {
                    return true;
                }
            }
        }
        return false;
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
}