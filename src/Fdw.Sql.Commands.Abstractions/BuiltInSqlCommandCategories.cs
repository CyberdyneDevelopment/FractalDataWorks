using Fdw.Collections.Attributes;

namespace Fdw.Sql.Commands.Abstractions;

/// <summary>Built-in SQL command categories.</summary>
public static class BuiltInSqlCommandCategories
{
    /// <summary>Static analysis over .sqlproj scripts.</summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    [TypeOption(typeof(SqlCommandCategories), "Analysis")]
    public sealed class AnalysisCategory : SqlCommandCategoryBase
    { public AnalysisCategory() : base("Analysis", "Static analysis over .sqlproj scripts.") { } }

    /// <summary>Build / compile / emit DACPAC.</summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    [TypeOption(typeof(SqlCommandCategories), "Build")]
    public sealed class BuildCategory : SqlCommandCategoryBase
    { public BuildCategory() : base("Build", "Build / compile / emit DACPAC.") { } }

    /// <summary>Generate new tables / views / procedures / tests.</summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    [TypeOption(typeof(SqlCommandCategories), "Generation")]
    public sealed class GenerationCategory : SqlCommandCategoryBase
    { public GenerationCategory() : base("Generation", "Generate new tables / views / procedures / tests.") { } }

    /// <summary>Find references / dependencies / definitions.</summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    [TypeOption(typeof(SqlCommandCategories), "Navigation")]
    public sealed class NavigationCategory : SqlCommandCategoryBase
    { public NavigationCategory() : base("Navigation", "Find references / dependencies / definitions.") { } }

    /// <summary>Project structure: objects, files, references.</summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    [TypeOption(typeof(SqlCommandCategories), "Project")]
    public sealed class ProjectCategory : SqlCommandCategoryBase
    { public ProjectCategory() : base("Project", "Project structure: objects, files, references.") { } }

    /// <summary>Rename / extract / move with cascading edits.</summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    [TypeOption(typeof(SqlCommandCategories), "Refactoring")]
    public sealed class RefactoringCategory : SqlCommandCategoryBase
    { public RefactoringCategory() : base("Refactoring", "Rename / extract / move with cascading edits.") { } }

    /// <summary>Search across scripts: text / symbols / duplicates.</summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    [TypeOption(typeof(SqlCommandCategories), "Search")]
    public sealed class SearchCategory : SqlCommandCategoryBase
    { public SearchCategory() : base("Search", "Search across scripts: text / symbols / duplicates.") { } }

    /// <summary>Workspace state: snapshot / baseline / apply.</summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    [TypeOption(typeof(SqlCommandCategories), "Workspace")]
    public sealed class WorkspaceCategory : SqlCommandCategoryBase
    { public WorkspaceCategory() : base("Workspace", "Workspace state: snapshot / baseline / apply.") { } }
}
