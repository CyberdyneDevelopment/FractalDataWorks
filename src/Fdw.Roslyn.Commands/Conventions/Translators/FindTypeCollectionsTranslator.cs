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
using Fdw.Roslyn.Commands.Logging;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Fdw.Roslyn.Commands.Conventions.Translators;

/// <summary>
/// Translator for finding TypeCollection definitions.
/// </summary>
[TypeOption(typeof(RoslynCommandTranslators), "FindTypeCollections")]
public sealed class FindTypeCollectionsTranslator
    : RoslynCommandTranslatorBase<FindTypeCollectionsCommand, QueryResult<TypeCollectionsData>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FindTypeCollectionsTranslator"/> class.
    /// </summary>
    public FindTypeCollectionsTranslator()
        : base("FindTypeCollectionsTranslator", "Translates TypeCollection search commands")
    {
    }

    /// <inheritdoc/>
    public override async Task<IGenericResult<QueryResult<TypeCollectionsData>>> Translate(
        FindTypeCollectionsCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        FindTypeCollectionsTranslatorLog.Scanning(Logger);

        var typeCollections = new List<TypeCollectionInfo>();

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
                    var hasTypeCollectionAttr = classDecl.AttributeLists
                        .SelectMany(al => al.Attributes)
                        .Any(a => a.Name.ToString().Contains("TypeCollection", StringComparison.Ordinal));

                    if (hasTypeCollectionAttr)
                    {
                        if (semanticModel.GetDeclaredSymbol(classDecl, cancellationToken) is not INamedTypeSymbol symbol)
                            continue;

                        typeCollections.Add(new TypeCollectionInfo
                        {
                            Name = symbol.Name,
                            FullName = symbol.ToDisplayString(),
                            Project = project.Name,
                            FilePath = document.FilePath ?? document.Name,
                            IsAbstract = symbol.IsAbstract,
                            IsPartial = classDecl.Modifiers.Any(m => string.Equals(m.Text, "partial", StringComparison.Ordinal))
                        });
                    }
                }
            }
        }

        var data = new TypeCollectionsData
        {
            Count = typeCollections.Count,
            TypeCollections = typeCollections
        };

        var result = new QueryResult<TypeCollectionsData>(
            $"Found {typeCollections.Count} TypeCollections",
            data);

        FindTypeCollectionsTranslatorLog.Found(Logger, typeCollections.Count);

        return GenericResult<QueryResult<TypeCollectionsData>>.Success(result);
    }
}
