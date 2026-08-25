using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Roslyn.Commands.Abstractions;
using Fdw.Roslyn.Commands.Abstractions.Results;
using Fdw.Roslyn.Commands.Logging;
using Fdw.Roslyn.Commands.Navigation.Commands;
using Fdw.Roslyn.Commands.Navigation.Results;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Fdw.Roslyn.Commands.Navigation.Translators;

/// <summary>
/// Translator for FindMembers command.
/// </summary>
[TypeOption(typeof(RoslynCommandTranslators), "FindMembersTranslator")]
public sealed class FindMembersTranslator : RoslynCommandTranslatorBase<FindMembersCommand, QueryResult<IReadOnlyList<NavigationMemberInfo>>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FindMembersTranslator"/> class.
    /// </summary>
    public FindMembersTranslator()
        : base("FindMembersTranslator", "Translates FindMembers command to list type members")
    {
    }

    /// <inheritdoc/>
    public override async Task<IGenericResult<QueryResult<IReadOnlyList<NavigationMemberInfo>>>> Translate(
        FindMembersCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        FindMembersTranslatorLog.Finding(Logger, command.FilePath, command.Line, command.Column, command.IncludeInherited);

        var documentId = solution.GetDocumentIdsWithFilePath(command.FilePath).FirstOrDefault();
        if (documentId is null)
        {
            FindMembersTranslatorLog.DocumentNotFound(Logger, command.FilePath);
            return GenericResult<QueryResult<IReadOnlyList<NavigationMemberInfo>>>.Failure(
                RoslynResultCodes.ByName("DocumentNotFound"),
                ResultDetails.Create().With("FilePath", command.FilePath));
        }

        var document = solution.GetDocument(documentId);
        if (document is null)
        {
            FindMembersTranslatorLog.FailedToLoadDocument(Logger, command.FilePath);
            return GenericResult<QueryResult<IReadOnlyList<NavigationMemberInfo>>>.Failure(
                RoslynResultCodes.ByName("FailedToLoadDocument"));
        }

        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

        if (semanticModel is null || syntaxRoot is null)
        {
            FindMembersTranslatorLog.FailedToAnalyzeDocument(Logger, command.FilePath);
            return GenericResult<QueryResult<IReadOnlyList<NavigationMemberInfo>>>.Failure(
                RoslynResultCodes.ByName("FailedToAnalyzeDocument"));
        }

        var position = text.Lines.GetPosition(new LinePosition(command.Line - 1, command.Column - 1));
        var token = syntaxRoot.FindToken(position);
        var symbol = semanticModel.GetSymbolInfo(token.Parent!, cancellationToken).Symbol
                  ?? semanticModel.GetDeclaredSymbol(token.Parent!, cancellationToken);

        if (symbol is not INamedTypeSymbol typeSymbol)
        {
            FindMembersTranslatorLog.SymbolNotType(Logger, command.FilePath, command.Line, command.Column);
            return GenericResult<QueryResult<IReadOnlyList<NavigationMemberInfo>>>.Failure(
                RoslynResultCodes.ByName("SymbolNotType"));
        }

        var allowedKinds = ParseMemberKinds(command.MemberKinds);
        var members = new List<NavigationMemberInfo>();

        var symbolMembers = command.IncludeInherited
            ? typeSymbol.GetMembers()
            : typeSymbol.GetMembers().Where(m => SymbolEqualityComparer.Default.Equals(m.ContainingType, typeSymbol));

        foreach (var member in symbolMembers)
        {
            // Skip compiler-generated members
            if (member.IsImplicitlyDeclared)
                continue;

            // Apply kind filter
            if (allowedKinds.Count > 0 && !allowedKinds.Contains(member.Kind))
                continue;

            var memberInfo = CreateMemberInfo(member);
            members.Add(memberInfo);
        }

        var result = new QueryResult<IReadOnlyList<NavigationMemberInfo>>(
            $"Found {members.Count} member(s) in '{typeSymbol.Name}'",
            members);

        FindMembersTranslatorLog.Found(Logger, typeSymbol.Name, members.Count);

        return GenericResult<QueryResult<IReadOnlyList<NavigationMemberInfo>>>.Success(result);
    }

    private static NavigationMemberInfo CreateMemberInfo(ISymbol member)
    {
        var info = new NavigationMemberInfo
        {
            Name = member.Name,
            Kind = member.Kind.ToString(),
            Accessibility = member.DeclaredAccessibility.ToString(),
            IsStatic = member.IsStatic,
            IsAbstract = member.IsAbstract,
            IsVirtual = member.IsVirtual,
            IsOverride = member.IsOverride
        };

        if (member is IMethodSymbol method)
        {
            info = info with
            {
                ReturnType = method.ReturnType.ToDisplayString(),
                Parameters = method.Parameters.Select(p => $"{p.Type.ToDisplayString()} {p.Name}").ToList()
            };
        }
        else if (member is IPropertySymbol property)
        {
            info = info with
            {
                PropertyType = property.Type.ToDisplayString(),
                HasGetter = property.GetMethod is not null,
                HasSetter = property.SetMethod is not null
            };
        }
        else if (member is IFieldSymbol field)
        {
            info = info with
            {
                FieldType = field.Type.ToDisplayString(),
                IsReadOnly = field.IsReadOnly,
                IsConst = field.IsConst
            };
        }

        if (member.Locations.Length > 0 && member.Locations[0].IsInSource)
        {
            var lineSpan = member.Locations[0].GetLineSpan();
            info = info with
            {
                FilePath = lineSpan.Path ?? string.Empty,
                Line = lineSpan.StartLinePosition.Line + 1,
                Column = lineSpan.StartLinePosition.Character + 1
            };
        }

        return info;
    }

    private static HashSet<SymbolKind> ParseMemberKinds(string? filter)
    {
        var kinds = new HashSet<SymbolKind>();
        if (string.IsNullOrWhiteSpace(filter))
            return kinds;

        foreach (var part in filter.Split(','))
        {
            var trimmed = part.Trim();
            if (Enum.TryParse<SymbolKind>(trimmed, ignoreCase: true, out var kind))
            {
                kinds.Add(kind);
            }
        }

        return kinds;
    }
}
