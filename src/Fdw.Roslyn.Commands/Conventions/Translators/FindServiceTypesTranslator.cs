using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Roslyn.Commands.Abstractions;
using Fdw.Roslyn.Commands.Abstractions.Results;
using Fdw.Roslyn.Commands.Conventions.Commands;
using Fdw.Roslyn.Commands.Conventions.Results;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Fdw.Roslyn.Commands.Conventions.Translators;

/// <summary>
/// Translator for finding ServiceType implementations.
/// </summary>
[TypeOption(typeof(RoslynCommandTranslators), "FindServiceTypes")]
public sealed class FindServiceTypesTranslator
    : RoslynCommandTranslatorBase<FindServiceTypesCommand, QueryResult<ServiceTypesData>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FindServiceTypesTranslator"/> class.
    /// </summary>
    public FindServiceTypesTranslator()
        : base("FindServiceTypesTranslator", "Translates ServiceType search commands")
    {
    }

    /// <inheritdoc/>
    public override async Task<IGenericResult<QueryResult<ServiceTypesData>>> Translate(
        FindServiceTypesCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        var serviceTypes = new List<ServiceTypeInfo>();

        foreach (var project in solution.Projects)
        {
            var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            if (compilation is null) continue;

            foreach (var document in project.Documents)
            {
                if (command.IsGeneratedDocument(document)) continue;

                var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                if (syntaxRoot is null) continue;

                var semanticModel = compilation.GetSemanticModel(syntaxRoot.SyntaxTree);

                var classDeclarations = syntaxRoot.DescendantNodes().OfType<ClassDeclarationSyntax>();
                foreach (var classDecl in classDeclarations)
                {
                    var serviceTypeAttr = classDecl.AttributeLists
                        .SelectMany(al => al.Attributes)
                        .FirstOrDefault(a =>
                            a.Name.ToString().Contains("ServiceTypeOption", StringComparison.Ordinal) ||
                            a.Name.ToString().Contains("ServiceTypeCollection", StringComparison.Ordinal));

                    if (serviceTypeAttr is null) continue;

                    if (semanticModel.GetDeclaredSymbol(classDecl, cancellationToken) is not INamedTypeSymbol symbol)
                        continue;

                    var isCollection = serviceTypeAttr.Name.ToString().Contains("Collection", StringComparison.Ordinal);

                    serviceTypes.Add(new ServiceTypeInfo
                    {
                        Name = symbol.Name,
                        FullName = symbol.ToDisplayString(),
                        Project = project.Name,
                        FilePath = document.FilePath ?? document.Name,
                        IsCollection = isCollection,
                        IsSealed = symbol.IsSealed,
                        HasRegisterMethod = symbol.GetMembers("Register").Any()
                    });
                }
            }
        }

        var data = new ServiceTypesData
        {
            Count = serviceTypes.Count,
            ServiceTypes = serviceTypes
        };

        var result = new QueryResult<ServiceTypesData>(
            $"Found {serviceTypes.Count} ServiceTypes",
            data);

        return GenericResult<QueryResult<ServiceTypesData>>.Success(result);
    }
}
