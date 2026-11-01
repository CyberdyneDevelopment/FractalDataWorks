using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Fdw.Conventions.FileSplitter;

/// <summary>
/// CLI tool that splits C# files containing multiple top-level type declarations
/// into one file per type, fixing FDW005 violations.
///
/// Usage:
///   dotnet fdw-split &lt;path&gt; [--dry-run]
///
/// Where &lt;path&gt; is a .cs file, a directory, or a .csproj file.
/// When given a .csproj, it finds all .cs files in the project directory.
/// </summary>
internal static class Program
{
    private static int Main(string[] args)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help")
        {
            PrintUsage();
            return args.Length == 0 ? 1 : 0;
        }

        var path = args[0];
        var dryRun = args.Any(a => string.Equals(a, "--dry-run", StringComparison.OrdinalIgnoreCase));

        var files = ResolveFiles(path);
        if (files.Count == 0)
        {
            Console.Error.WriteLine($"No .cs files found for path: {path}");
            return 1;
        }

        var totalCreated = 0;
        var totalModified = 0;

        foreach (var file in files)
        {
            var result = ProcessFile(file, dryRun);
            totalCreated += result.FilesCreated;
            if (result.FilesCreated > 0)
                totalModified++;
        }

        if (totalCreated == 0)
        {
            Console.WriteLine("No files needed splitting.");
        }
        else
        {
            var action = dryRun ? "Would create" : "Created";
            Console.WriteLine($"{action} {totalCreated} new file(s) from {totalModified} source file(s).");
        }

        return 0;
    }

    private static void PrintUsage()
    {
        Console.WriteLine("FDW005 File Splitter - splits multi-type .cs files into one file per type.");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  dotnet fdw-split <path> [--dry-run]");
        Console.WriteLine();
        Console.WriteLine("Arguments:");
        Console.WriteLine("  <path>      A .cs file, a directory, or a .csproj file");
        Console.WriteLine("  --dry-run   Show what would be done without making changes");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  dotnet fdw-split src/MyProject/Results/ResultCodes.cs");
        Console.WriteLine("  dotnet fdw-split src/MyProject/");
        Console.WriteLine("  dotnet fdw-split src/MyProject/MyProject.csproj");
    }

    private static List<string> ResolveFiles(string path)
    {
        if (File.Exists(path))
        {
            if (path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                return [path];

            if (path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            {
                var dir = Path.GetDirectoryName(path) ?? ".";
                return Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories)
                    .Where(f => !IsInObjOrBin(f, dir))
                    .ToList();
            }

            return [];
        }

        if (Directory.Exists(path))
        {
            return Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories)
                .Where(f => !IsInObjOrBin(f, path))
                .ToList();
        }

        return [];
    }

    private static bool IsInObjOrBin(string filePath, string rootDir)
    {
        var relative = Path.GetRelativePath(rootDir, filePath);
        return relative.StartsWith("obj" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            || relative.StartsWith("bin" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            || relative.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            || relative.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

    private static SplitResult ProcessFile(string filePath, bool dryRun)
    {
        var sourceText = File.ReadAllText(filePath);
        var tree = CSharpSyntaxTree.ParseText(sourceText, path: filePath);
        var root = tree.GetCompilationUnitRoot();

        // Find all top-level type declarations (not nested inside another type)
        var topLevelTypes = GetTopLevelTypes(root);
        if (topLevelTypes.Count <= 1)
            return new SplitResult(0);

        var fileName = Path.GetFileName(filePath);
        var primaryName = GetPrimaryFileName(fileName);
        var directory = Path.GetDirectoryName(filePath) ?? ".";

        // Determine which types don't match the file name
        var typesToExtract = new List<BaseTypeDeclarationSyntax>();
        foreach (var typeDecl in topLevelTypes)
        {
            var typeName = typeDecl.Identifier.Text;
            if (!string.Equals(typeName, primaryName, StringComparison.Ordinal))
            {
                typesToExtract.Add(typeDecl);
            }
        }

        if (typesToExtract.Count == 0)
            return new SplitResult(0);

        // Collect usings and extern aliases from the compilation unit
        var usings = root.Usings;
        var externs = root.Externs;

        var filesCreated = 0;

        foreach (var typeDecl in typesToExtract)
        {
            var typeName = typeDecl.Identifier.Text;
            var targetPath = Path.Combine(directory, typeName + ".cs");

            if (dryRun)
            {
                if (File.Exists(targetPath))
                    Console.WriteLine($"  [merge]  {typeName} -> {Path.GetRelativePath(".", targetPath)}");
                else
                    Console.WriteLine($"  [create] {typeName} -> {Path.GetRelativePath(".", targetPath)}");
            }
            else
            {
                if (File.Exists(targetPath))
                {
                    MergeIntoExistingFile(targetPath, usings, typeDecl);
                }
                else
                {
                    var newContent = BuildFileForType(usings, externs, typeDecl);
                    File.WriteAllText(targetPath, newContent);
                }
            }

            filesCreated++;
        }

        // Remove extracted types from the original file
        if (!dryRun)
        {
            var newSourceRoot = root.RemoveNodes(typesToExtract, SyntaxRemoveOptions.KeepLeadingTrivia)!;
            var cleanedSource = CleanupBlankLines(newSourceRoot.ToFullString());
            File.WriteAllText(filePath, cleanedSource);
        }

        var relPath = Path.GetRelativePath(".", filePath);
        var actionVerb = dryRun ? "Would extract" : "Extracted";
        Console.WriteLine($"{actionVerb} {typesToExtract.Count} type(s) from {relPath}");

        return new SplitResult(filesCreated);
    }

    private static string BuildFileForType(
        SyntaxList<UsingDirectiveSyntax> usings,
        SyntaxList<ExternAliasDirectiveSyntax> externs,
        BaseTypeDeclarationSyntax typeDecl)
    {
        var containingNamespace = typeDecl.Ancestors()
            .OfType<BaseNamespaceDeclarationSyntax>().FirstOrDefault();

        var typeWithTrivia = typeDecl.WithLeadingTrivia(GetSignificantLeadingTrivia(typeDecl));
        var typeAsMember = (MemberDeclarationSyntax)typeWithTrivia;

        CompilationUnitSyntax newRoot;

        if (containingNamespace is FileScopedNamespaceDeclarationSyntax fileScopedNs)
        {
            var newNs = fileScopedNs
                .WithMembers(SyntaxFactory.SingletonList(typeAsMember))
                .WithLeadingTrivia(SyntaxFactory.TriviaList(SyntaxFactory.LineFeed))
                .WithTrailingTrivia(SyntaxFactory.TriviaList(SyntaxFactory.LineFeed));

            newRoot = SyntaxFactory.CompilationUnit()
                .WithExterns(externs)
                .WithUsings(usings)
                .WithMembers(SyntaxFactory.SingletonList<MemberDeclarationSyntax>(newNs))
                .NormalizeWhitespace()
                .WithTrailingTrivia(SyntaxFactory.TriviaList(SyntaxFactory.LineFeed));
        }
        else if (containingNamespace is NamespaceDeclarationSyntax blockNs)
        {
            var newNs = blockNs
                .WithMembers(SyntaxFactory.SingletonList(typeAsMember))
                .WithLeadingTrivia(SyntaxFactory.TriviaList(SyntaxFactory.LineFeed))
                .WithTrailingTrivia(SyntaxFactory.TriviaList(SyntaxFactory.LineFeed));

            newRoot = SyntaxFactory.CompilationUnit()
                .WithExterns(externs)
                .WithUsings(usings)
                .WithMembers(SyntaxFactory.SingletonList<MemberDeclarationSyntax>(newNs))
                .NormalizeWhitespace()
                .WithTrailingTrivia(SyntaxFactory.TriviaList(SyntaxFactory.LineFeed));
        }
        else
        {
            newRoot = SyntaxFactory.CompilationUnit()
                .WithExterns(externs)
                .WithUsings(usings)
                .WithMembers(SyntaxFactory.SingletonList(typeAsMember))
                .NormalizeWhitespace()
                .WithTrailingTrivia(SyntaxFactory.TriviaList(SyntaxFactory.LineFeed));
        }

        return newRoot.ToFullString();
    }

    private static void MergeIntoExistingFile(
        string targetPath,
        SyntaxList<UsingDirectiveSyntax> sourceUsings,
        BaseTypeDeclarationSyntax typeDecl)
    {
        var existingText = File.ReadAllText(targetPath);
        var existingTree = CSharpSyntaxTree.ParseText(existingText, path: targetPath);
        var existingRoot = existingTree.GetCompilationUnitRoot();

        var mergedUsings = MergeUsings(existingRoot.Usings, sourceUsings);

        var typeAsMember = (MemberDeclarationSyntax)typeDecl
            .WithLeadingTrivia(GetSignificantLeadingTrivia(typeDecl));

        var sourceNs = typeDecl.Ancestors()
            .OfType<BaseNamespaceDeclarationSyntax>().FirstOrDefault();
        var sourceNsName = sourceNs?.Name.ToString();

        CompilationUnitSyntax updatedRoot;

        if (sourceNsName != null)
        {
            var matchingNs = existingRoot.Members
                .OfType<BaseNamespaceDeclarationSyntax>()
                .FirstOrDefault(ns => string.Equals(ns.Name.ToString(), sourceNsName, StringComparison.Ordinal));

            if (matchingNs != null)
            {
                var updated = matchingNs.AddMembers(typeAsMember);
                updatedRoot = existingRoot.ReplaceNode(matchingNs, updated).WithUsings(mergedUsings);
            }
            else
            {
                var newNs = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName(sourceNsName))
                    .WithMembers(SyntaxFactory.SingletonList(typeAsMember));
                updatedRoot = existingRoot.AddMembers(newNs).WithUsings(mergedUsings);
            }
        }
        else
        {
            updatedRoot = existingRoot.AddMembers(typeAsMember).WithUsings(mergedUsings);
        }

        File.WriteAllText(targetPath, updatedRoot.NormalizeWhitespace().ToFullString());
    }

    private static SyntaxList<UsingDirectiveSyntax> MergeUsings(
        SyntaxList<UsingDirectiveSyntax> existing,
        SyntaxList<UsingDirectiveSyntax> incoming)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var u in existing)
            names.Add(u.Name?.ToString() ?? u.ToString());

        var merged = new List<UsingDirectiveSyntax>(existing);
        foreach (var u in incoming)
        {
            var name = u.Name?.ToString() ?? u.ToString();
            if (names.Add(name))
                merged.Add(u);
        }

        return SyntaxFactory.List(merged);
    }

    private static SyntaxTriviaList GetSignificantLeadingTrivia(BaseTypeDeclarationSyntax typeDecl)
    {
        var trivia = new List<SyntaxTrivia>();
        foreach (var t in typeDecl.GetLeadingTrivia())
        {
            if (t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia)
                || t.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia)
                || t.IsKind(SyntaxKind.PragmaWarningDirectiveTrivia)
                || t.IsKind(SyntaxKind.RegionDirectiveTrivia)
                || t.IsKind(SyntaxKind.EndRegionDirectiveTrivia)
                || t.IsKind(SyntaxKind.SingleLineCommentTrivia)
                || t.IsKind(SyntaxKind.MultiLineCommentTrivia)
                || t.IsKind(SyntaxKind.WhitespaceTrivia)
                || t.IsKind(SyntaxKind.EndOfLineTrivia))
            {
                trivia.Add(t);
            }
        }

        return SyntaxFactory.TriviaList(trivia);
    }

    private static List<BaseTypeDeclarationSyntax> GetTopLevelTypes(SyntaxNode root)
    {
        var result = new List<BaseTypeDeclarationSyntax>();
        foreach (var node in root.DescendantNodes())
        {
            if (node is BaseTypeDeclarationSyntax typeDecl && !IsNestedType(typeDecl))
                result.Add(typeDecl);
        }

        return result;
    }

    private static bool IsNestedType(BaseTypeDeclarationSyntax typeDecl)
    {
        var parent = typeDecl.Parent;
        while (parent != null)
        {
            if (parent is BaseTypeDeclarationSyntax)
                return true;
            if (parent is BaseNamespaceDeclarationSyntax || parent is CompilationUnitSyntax)
                return false;
            parent = parent.Parent;
        }

        return false;
    }

    private static string GetPrimaryFileName(string fileName)
    {
        var withoutExtension = Path.GetFileNameWithoutExtension(fileName);
        if (string.IsNullOrEmpty(withoutExtension))
            return string.Empty;

        var dotIndex = withoutExtension.IndexOf('.');
        return dotIndex >= 0 ? withoutExtension[..dotIndex] : withoutExtension;
    }

    private static string CleanupBlankLines(string text)
    {
        var lines = text.Split('\n');
        var result = new List<string>(lines.Length);
        var blankCount = 0;

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                blankCount++;
                if (blankCount <= 2)
                    result.Add(line);
            }
            else
            {
                blankCount = 0;
                result.Add(line);
            }
        }

        return string.Join("\n", result);
    }

    private readonly record struct SplitResult(int FilesCreated);
}
