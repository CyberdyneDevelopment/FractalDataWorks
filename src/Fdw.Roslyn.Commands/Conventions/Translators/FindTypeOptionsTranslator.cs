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
/// Translator for finding TypeOption implementations.
/// </summary>
[TypeOption(typeof(RoslynCommandTranslators), "FindTypeOptions")]
public sealed class FindTypeOptionsTranslator
    : RoslynCommandTranslatorBase<FindTypeOptionsCommand, QueryResult<TypeOptionsData>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FindTypeOptionsTranslator"/> class.
    /// </summary>
    public FindTypeOptionsTranslator()
        : base("FindTypeOptionsTranslator", "Translates TypeOption search commands")
    {
    }

    /// <inheritdoc/>
#pragma warning disable MA0051 // Linear Roslyn flow: iterate projects, find TypeOption attributes, collect results
    public override async Task<IGenericResult<QueryResult<TypeOptionsData>>> Translate(
        FindTypeOptionsCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        var typeOptions = new List<TypeOptionInfo>();

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
                    var typeOptionAttr = classDecl.AttributeLists
                        .SelectMany(al => al.Attributes)
                        .FirstOrDefault(a => a.Name.ToString().Contains("TypeOption", StringComparison.Ordinal));

                    if (typeOptionAttr is null) continue;

                    // Extract collection type from attribute
                    var attrArgs = typeOptionAttr.ArgumentList?.Arguments;
                    if (attrArgs is null || attrArgs.Value.Count < 2) continue;

                    var collectionArg = attrArgs.Value[0].Expression.ToString();
                    var optionName = attrArgs.Value[1].Expression.ToString().Trim('"');

                    // Filter by collection name if specified
                    if (!string.IsNullOrEmpty(command.CollectionName) &&
                        !collectionArg.Contains(command.CollectionName, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (semanticModel.GetDeclaredSymbol(classDecl, cancellationToken) is not INamedTypeSymbol symbol)
                        continue;

                    typeOptions.Add(new TypeOptionInfo
                    {
                        Name = symbol.Name,
                        OptionName = optionName,
                        FullName = symbol.ToDisplayString(),
                        Collection = collectionArg,
                        Project = project.Name,
                        FilePath = document.FilePath ?? document.Name,
                        IsSealed = symbol.IsSealed
                    });
                }
            }
        }

        var displayName = string.IsNullOrEmpty(command.CollectionName) ? "all collections" : command.CollectionName;

        var data = new TypeOptionsData
        {
            Count = typeOptions.Count,
            CollectionFilter = command.CollectionName ?? "(all)",
            TypeOptions = typeOptions
        };

        var result = new QueryResult<TypeOptionsData>(
            $"Found {typeOptions.Count} TypeOptions for {displayName}",
            data);

        return GenericResult<QueryResult<TypeOptionsData>>.Success(result);
    }
#pragma warning restore MA0051
}
