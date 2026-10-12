using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;
using Fdw.Roslyn.Commands.Abstractions;
using Fdw.Roslyn.Commands.Abstractions.Results;
using Fdw.Roslyn.Commands.Logging;
using Fdw.Roslyn.Commands.Refactoring.Commands;
using Fdw.Roslyn.Commands.Refactoring.Results;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using MsCompilation = Microsoft.CodeAnalysis.Compilation;

namespace Fdw.Roslyn.Commands.Refactoring.Translators;

/// <summary>
/// Translator for <see cref="ResolveInheritDocCommand"/>. Expands <c>&lt;inheritdoc/&gt;</c> comments
/// using <see cref="ISymbol.GetDocumentationCommentXml(System.Globalization.CultureInfo, bool, CancellationToken)"/>,
/// which performs Roslyn's own inheritdoc resolution (override/interface chain plus explicit cref).
/// </summary>
[TypeOption(typeof(RoslynCommandTranslators), "ResolveInheritDoc")]
public sealed class ResolveInheritDocTranslator : RoslynCommandTranslatorBase<ResolveInheritDocCommand, MutationResult<ResolveInheritDocResult>>
{
    private static readonly HashSet<string> MeaningfulTags = new(StringComparer.Ordinal)
    {
        "summary", "param", "returns", "typeparam", "value", "remarks", "example", "exception", "seealso",
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="ResolveInheritDocTranslator"/> class.
    /// </summary>
    public ResolveInheritDocTranslator()
        : base("ResolveInheritDoc", "Expands <inheritdoc/> comments using Roslyn's documentation resolution")
    {
    }

    /// <inheritdoc/>
    public override async Task<IGenericResult<MutationResult<ResolveInheritDocResult>>> Translate(
        ResolveInheritDocCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        ResolveInheritDocTranslatorLog.Resolving(Logger, command.FilePath ?? string.Empty, command.ProjectName ?? string.Empty);

        var scope = SelectScope(command, solution);
        if (!scope.IsSuccess)
            return GenericResult<MutationResult<ResolveInheritDocResult>>.Failure(scope.Code!, scope.Details);

        var filesScanned = 0;
        var sitesResolved = 0;
        var unresolved = new List<UnresolvedSite>();
        var changedFiles = new List<FileChange>();
        var processedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var currentSolution = solution;

        foreach (var documentId in scope.DocumentIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var document = currentSolution.GetDocument(documentId);
            if (document is null || IsSkippable(document.FilePath))
                continue;

            if (!processedPaths.Add(document.FilePath!))
                continue;

            filesScanned++;

            var processed = await ProcessDocument(document, cancellationToken).ConfigureAwait(false);
            unresolved.AddRange(processed.Unresolved);

            if (processed.ResolvedCount > 0 && processed.NewSolution is not null)
            {
                sitesResolved += processed.ResolvedCount;
                currentSolution = processed.NewSolution;
                changedFiles.Add(new FileChange(document.FilePath!, FileChangeTypes.Modified, document.Project.Name)
                {
                    TextChangeCount = processed.ResolvedCount,
                });
            }
        }

        var result = new ResolveInheritDocResult(filesScanned, changedFiles.Count, sitesResolved, unresolved.Count, unresolved);
        var summary = $"Resolved {sitesResolved} inheritdoc site(s) across {changedFiles.Count} file(s); {unresolved.Count} unresolved.";

        ResolveInheritDocTranslatorLog.Resolved(Logger, sitesResolved, changedFiles.Count, unresolved.Count);

        return GenericResult<MutationResult<ResolveInheritDocResult>>.Success(
            new MutationResult<ResolveInheritDocResult>(summary, currentSolution, changedFiles, result));
    }

    private ScopeSelection SelectScope(ResolveInheritDocCommand command, Solution solution)
    {
        if (!string.IsNullOrEmpty(command.FilePath))
        {
            var documentId = solution.GetDocumentIdsWithFilePath(command.FilePath).FirstOrDefault();
            if (documentId is null)
            {
                ResolveInheritDocTranslatorLog.DocumentNotFound(Logger, command.FilePath);
                return ScopeSelection.Fail(RoslynResultCodes.ByName("DocumentNotFound"), ResultDetails.Create().With("FilePath", command.FilePath));
            }

            return ScopeSelection.Ok(new[] { documentId });
        }

        if (!string.IsNullOrEmpty(command.ProjectName))
        {
            var project = solution.Projects.FirstOrDefault(p => string.Equals(p.Name, command.ProjectName, StringComparison.Ordinal));
            if (project is null)
            {
                ResolveInheritDocTranslatorLog.ProjectNotFound(Logger, command.ProjectName);
                return ScopeSelection.Fail(RoslynResultCodes.ByName("ProjectNotFound"), ResultDetails.Create().With("ProjectName", command.ProjectName));
            }

            return ScopeSelection.Ok(project.DocumentIds);
        }

        return ScopeSelection.Ok(solution.Projects.SelectMany(p => p.DocumentIds).ToList());
    }

    private static async Task<DocumentProcessResult> ProcessDocument(Document document, CancellationToken cancellationToken)
    {
        var unresolved = new List<UnresolvedSite>();
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

        if (root is null || model is null)
            return new DocumentProcessResult(null, 0, unresolved);

        var docComments = root.DescendantNodes(descendIntoTrivia: true).OfType<DocumentationCommentTriviaSyntax>().ToList();
        if (docComments.Count == 0)
            return new DocumentProcessResult(null, 0, unresolved);

        var filePath = document.FilePath!;
        var changes = new List<TextChange>();

        foreach (var docComment in docComments)
        {
            var inheritNodes = FindInheritDocNodes(docComment);
            if (inheritNodes.Count == 0)
                continue;

            var symbol = GetDocumentedSymbol(docComment, model, cancellationToken);
            if (symbol is null)
            {
                unresolved.AddRange(inheritNodes.Select(n => MakeSite(text, filePath, n, "(unknown)", UnresolvedReason.Other)));
                continue;
            }

            foreach (var node in inheritNodes)
            {
                var resolution = ResolveInheritDoc(symbol, node, model, cancellationToken);
                if (resolution.Doc is not null)
                {
                    var replacement = RenderReplacement(resolution.Doc, ComputeIndentSlashes(text, node));
                    if (!string.IsNullOrEmpty(replacement))
                        changes.Add(new TextChange(node.Span, replacement));
                }
                else
                {
                    unresolved.Add(MakeSite(text, filePath, node, symbol.ToDisplayString(), resolution.Reason));
                }
            }
        }

        if (changes.Count == 0)
            return new DocumentProcessResult(null, 0, unresolved);

        changes.Sort((a, b) => a.Span.Start.CompareTo(b.Span.Start));
        var newSolution = document.WithText(text.WithChanges(changes)).Project.Solution;
        return new DocumentProcessResult(newSolution, changes.Count, unresolved);
    }

    private static List<XmlNodeSyntax> FindInheritDocNodes(DocumentationCommentTriviaSyntax docComment)
    {
        var nodes = new List<XmlNodeSyntax>();
        foreach (var node in docComment.Content)
        {
            var localName = node switch
            {
                XmlEmptyElementSyntax empty => empty.Name?.LocalName.ValueText,
                XmlElementSyntax element => element.StartTag.Name?.LocalName.ValueText,
                _ => null,
            };

            if (string.Equals(localName, "inheritdoc", StringComparison.OrdinalIgnoreCase))
                nodes.Add(node);
        }

        return nodes;
    }

    private static ISymbol? GetDocumentedSymbol(DocumentationCommentTriviaSyntax docComment, SemanticModel model, CancellationToken cancellationToken)
    {
        var member = docComment.ParentTrivia.Token.Parent?.FirstAncestorOrSelf<MemberDeclarationSyntax>();
        if (member is null)
            return null;

        var symbol = model.GetDeclaredSymbol(member, cancellationToken);
        if (symbol is not null)
            return symbol;

        if (member is BaseFieldDeclarationSyntax field && field.Declaration.Variables.Count > 0)
            return model.GetDeclaredSymbol(field.Declaration.Variables[0], cancellationToken);

        return null;
    }

    private static XElement? TryParseResolved(string? resolvedXml)
    {
        if (string.IsNullOrWhiteSpace(resolvedXml))
            return null;

        try
        {
            return XDocument.Parse(resolvedXml, LoadOptions.PreserveWhitespace).Root;
        }
        catch (XmlException ex)
        {
            _ = ex.Message;
            return null;
        }
    }

    private static bool HasMeaningfulDoc(XElement memberRoot) =>
        memberRoot.Elements().Any(e =>
            MeaningfulTags.Contains(e.Name.LocalName) &&
            (!string.IsNullOrWhiteSpace(e.Value) || e.HasAttributes));

    private static string RenderReplacement(XElement memberRoot, string indentSlashes)
    {
        var builder = new StringBuilder();
        var first = true;

        foreach (var element in memberRoot.Elements().Where(e => MeaningfulTags.Contains(e.Name.LocalName)))
        {
            var rendered = element.ToString(SaveOptions.DisableFormatting).Replace("\r\n", "\n", StringComparison.Ordinal).Replace("\r", "\n", StringComparison.Ordinal);
            foreach (var rawLine in rendered.Split('\n'))
            {
                var trimmed = rawLine.Trim();
                if (first)
                {
                    builder.Append(trimmed);
                    first = false;
                    continue;
                }

                builder.Append('\n').Append(indentSlashes);
                if (trimmed.Length > 0)
                    builder.Append(' ').Append(trimmed);
            }
        }

        return builder.ToString();
    }

    private static string ComputeIndentSlashes(SourceText text, SyntaxNode node)
    {
        var lineText = text.Lines.GetLineFromPosition(node.SpanStart).ToString();
        var slashIndex = lineText.IndexOf("///", StringComparison.Ordinal);
        return slashIndex >= 0 ? string.Concat(lineText.AsSpan(0, slashIndex), "///") : "///";
    }

    private static Resolution ResolveInheritDoc(ISymbol symbol, XmlNodeSyntax node, SemanticModel model, CancellationToken cancellationToken)
    {
        var visited = new HashSet<ISymbol>(SymbolEqualityComparer.Default) { symbol };

        var crefAttribute = GetCrefAttribute(node);
        if (crefAttribute is not null)
        {
            var target = model.GetSymbolInfo(crefAttribute.Cref, cancellationToken).Symbol;
            return target is null
                ? Resolution.Failed(UnresolvedReason.CrefTargetNotFound)
                : ResolveFrom(target, model.Compilation, visited, cancellationToken);
        }

        var baseMember = FindBaseMember(symbol);
        return baseMember is null
            ? Resolution.Failed(UnresolvedReason.NoBaseMember)
            : ResolveFrom(baseMember, model.Compilation, visited, cancellationToken);
    }

    private static Resolution ResolveFrom(ISymbol target, MsCompilation compilation, HashSet<ISymbol> visited, CancellationToken cancellationToken)
    {
        if (!visited.Add(target))
            return Resolution.Failed(UnresolvedReason.CircularInheritDoc);

        var root = TryParseResolved(target.GetDocumentationCommentXml(preferredCulture: null, expandIncludes: true, cancellationToken));
        if (root is not null && HasMeaningfulDoc(root))
            return Resolution.Resolved(root);

        // The target itself has no concrete docs (none, or only its own <inheritdoc/>) — follow it further.
        var crefFromXml = root is null ? null : GetInheritdocCrefFromXml(root);
        if (crefFromXml is not null)
        {
            var next = DocumentationCommentId.GetFirstSymbolForDeclarationId(crefFromXml, compilation);
            return next is null
                ? Resolution.Failed(UnresolvedReason.CrefTargetNotFound)
                : ResolveFrom(next, compilation, visited, cancellationToken);
        }

        var nextBase = FindBaseMember(target);
        return nextBase is null
            ? Resolution.Failed(UnresolvedReason.BaseHasNoDocs)
            : ResolveFrom(nextBase, compilation, visited, cancellationToken);
    }

    private static string? GetInheritdocCrefFromXml(XElement memberRoot) =>
        memberRoot.Elements()
            .FirstOrDefault(e => string.Equals(e.Name.LocalName, "inheritdoc", StringComparison.OrdinalIgnoreCase))
            ?.Attribute("cref")?.Value;

    private static ISymbol? FindBaseMember(ISymbol symbol) => symbol switch
    {
        IMethodSymbol method => method.OverriddenMethod ?? FindInterfaceMember(method),
        IPropertySymbol property => property.OverriddenProperty ?? FindInterfaceMember(property),
        IEventSymbol @event => @event.OverriddenEvent ?? FindInterfaceMember(@event),
        INamedTypeSymbol type => type.BaseType is not null && type.BaseType.SpecialType != SpecialType.System_Object
            ? type.BaseType
            : type.Interfaces.FirstOrDefault(),
        _ => null,
    };

    private static ISymbol? FindInterfaceMember(ISymbol member)
    {
        var explicitImpl = member switch
        {
            IMethodSymbol m when m.ExplicitInterfaceImplementations.Length > 0 => (ISymbol)m.ExplicitInterfaceImplementations[0],
            IPropertySymbol p when p.ExplicitInterfaceImplementations.Length > 0 => p.ExplicitInterfaceImplementations[0],
            IEventSymbol e when e.ExplicitInterfaceImplementations.Length > 0 => e.ExplicitInterfaceImplementations[0],
            _ => null,
        };
        if (explicitImpl is not null)
            return explicitImpl;

        var containingType = member.ContainingType;
        if (containingType is null)
            return null;

        foreach (var iface in containingType.AllInterfaces)
        {
            foreach (var interfaceMember in iface.GetMembers())
            {
                if (SymbolEqualityComparer.Default.Equals(containingType.FindImplementationForInterfaceMember(interfaceMember), member))
                    return interfaceMember;
            }
        }

        return null;
    }

    private static XmlCrefAttributeSyntax? GetCrefAttribute(XmlNodeSyntax node)
    {
        var attributes = node switch
        {
            XmlEmptyElementSyntax empty => empty.Attributes,
            XmlElementSyntax element => element.StartTag.Attributes,
            _ => default,
        };

        return attributes.OfType<XmlCrefAttributeSyntax>().FirstOrDefault();
    }

    private static UnresolvedSite MakeSite(SourceText text, string filePath, SyntaxNode node, string symbolDisplayName, UnresolvedReason reason)
    {
        var position = text.Lines.GetLinePosition(node.SpanStart);
        return new UnresolvedSite(filePath, position.Line + 1, position.Character + 1, symbolDisplayName, reason);
    }

    private static bool IsSkippable(string? filePath)
    {
        if (string.IsNullOrEmpty(filePath) || !filePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            return true;

        if (filePath.Contains($"{System.IO.Path.DirectorySeparatorChar}obj{System.IO.Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            return true;

        return filePath.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase)
            || filePath.EndsWith(".g.i.cs", StringComparison.OrdinalIgnoreCase)
            || filePath.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase)
            || filePath.EndsWith(".AssemblyInfo.cs", StringComparison.OrdinalIgnoreCase)
            || filePath.EndsWith(".GlobalUsings.g.cs", StringComparison.OrdinalIgnoreCase);
    }

    private sealed class ScopeSelection
    {
        private ScopeSelection(IReadOnlyList<DocumentId> documentIds, bool isSuccess, IResultCode? code, IResultDetails? details)
        {
            DocumentIds = documentIds;
            IsSuccess = isSuccess;
            Code = code;
            Details = details;
        }

        public IReadOnlyList<DocumentId> DocumentIds { get; }

        public bool IsSuccess { get; }

        public IResultCode? Code { get; }

        public IResultDetails? Details { get; }

        public static ScopeSelection Ok(IReadOnlyList<DocumentId> documentIds) => new(documentIds, true, null, null);

        public static ScopeSelection Fail(IResultCode code, IResultDetails details) => new(Array.Empty<DocumentId>(), false, code, details);
    }

    private sealed class DocumentProcessResult
    {
        public DocumentProcessResult(Solution? newSolution, int resolvedCount, IReadOnlyList<UnresolvedSite> unresolved)
        {
            NewSolution = newSolution;
            ResolvedCount = resolvedCount;
            Unresolved = unresolved;
        }

        public Solution? NewSolution { get; }

        public int ResolvedCount { get; }

        public IReadOnlyList<UnresolvedSite> Unresolved { get; }
    }

    private sealed class Resolution
    {
        private Resolution(XElement? doc, UnresolvedReason reason)
        {
            Doc = doc;
            Reason = reason;
        }

        public XElement? Doc { get; }

        public UnresolvedReason Reason { get; }

        public static Resolution Resolved(XElement doc) => new(doc, UnresolvedReason.Other);

        public static Resolution Failed(UnresolvedReason reason) => new(null, reason);
    }
}
