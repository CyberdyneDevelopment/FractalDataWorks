using Fdw.Collections.Attributes;

namespace Fdw.Sql.Commands.Abstractions;

/// <summary>Built-in SQL command categories.</summary>
public static class BuiltInSqlCommandCategories
{
    /// <summary>Static analysis over .sqlproj scripts.</summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    [TypeOption(typeof(SqlCommandCategories), "Analysis")]
    public sealed class AnalysisCategory : SqlCommandCategoryBase
    { public AnalysisCategory() : base(1, "Analysis", "Static analysis over .sqlproj scripts.") { } }

    /// <summary>Build / compile / emit DACPAC.</summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    [TypeOption(typeof(SqlCommandCategories), "Build")]
    public sealed class BuildCategory : SqlCommandCategoryBase
    { public BuildCategory() : base(2, "Build", "Build / compile / emit DACPAC.") { } }

    /// <summary>Generate new tables / views / procedures / tests.</summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    [TypeOption(typeof(SqlCommandCategories), "Generation")]
    public sealed class GenerationCategory : SqlCommandCategoryBase
    { public GenerationCategory() : base(3, "Generation", "Generate new tables / views / procedures / tests.") { } }

    /// <summary>Find references / dependencies / definitions.</summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    [TypeOption(typeof(SqlCommandCategories), "Navigation")]
    public sealed class NavigationCategory : SqlCommandCategoryBase
    { public NavigationCategory() : base(4, "Navigation", "Find references / dependencies / definitions.") { } }

    /// <summary>Project structure: objects, files, references.</summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    [TypeOption(typeof(SqlCommandCategories), "Project")]
    public sealed class ProjectCategory : SqlCommandCategoryBase
    { public ProjectCategory() : base(5, "Project", "Project structure: objects, files, references.") { } }

    /// <summary>Rename / extract / move with cascading edits.</summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    [TypeOption(typeof(SqlCommandCategories), "Refactoring")]
    public sealed class RefactoringCategory : SqlCommandCategoryBase
    { public RefactoringCategory() : base(6, "Refactoring", "Rename / extract / move with cascading edits.") { } }

    /// <summary>Search across scripts: text / symbols / duplicates.</summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    [TypeOption(typeof(SqlCommandCategories), "Search")]
    public sealed class SearchCategory : SqlCommandCategoryBase
    { public SearchCategory() : base(7, "Search", "Search across scripts: text / symbols / duplicates.") { } }

    /// <summary>Workspace state: snapshot / baseline / apply.</summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    [TypeOption(typeof(SqlCommandCategories), "Workspace")]
    public sealed class WorkspaceCategory : SqlCommandCategoryBase
    { public WorkspaceCategory() : base(8, "Workspace", "Workspace state: snapshot / baseline / apply.") { } }
}
