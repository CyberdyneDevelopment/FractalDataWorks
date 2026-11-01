namespace Fdw.Sql.Commands.Abstractions;

/// <summary>Static accessors for the built-in SQL command categories.</summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public static class StandardSqlCommandCategories
{
    /// <summary>Static analysis category.</summary>
    public static readonly ISqlCommandCategory Analysis    = new BuiltInSqlCommandCategories.AnalysisCategory();
    /// <summary>Build/compile/emit category.</summary>
    public static readonly ISqlCommandCategory Build       = new BuiltInSqlCommandCategories.BuildCategory();
    /// <summary>Code-generation category.</summary>
    public static readonly ISqlCommandCategory Generation  = new BuiltInSqlCommandCategories.GenerationCategory();
    /// <summary>Symbol-navigation category.</summary>
    public static readonly ISqlCommandCategory Navigation  = new BuiltInSqlCommandCategories.NavigationCategory();
    /// <summary>Project-structure category.</summary>
    public static readonly ISqlCommandCategory Project     = new BuiltInSqlCommandCategories.ProjectCategory();
    /// <summary>Refactoring category.</summary>
    public static readonly ISqlCommandCategory Refactoring = new BuiltInSqlCommandCategories.RefactoringCategory();
    /// <summary>Search category.</summary>
    public static readonly ISqlCommandCategory Search      = new BuiltInSqlCommandCategories.SearchCategory();
    /// <summary>Workspace-state category.</summary>
    public static readonly ISqlCommandCategory Workspace   = new BuiltInSqlCommandCategories.WorkspaceCategory();
}
