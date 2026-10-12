# Code Conventions Analyzers

FractalDataWorks enforces code conventions through a suite of custom Roslyn analyzers. These replace several third-party analyzers with FDW-specific behavior and configurable thresholds.

## Overview

| Rule | Replaces | Purpose |
|------|----------|---------|
| FDW005 | MA0048 | File name must match type name (with generic arity support) |
| FDW006 | MA0051 | Method too long (configurable line threshold) |
| FDW007 | — | Method too complex (cyclomatic complexity) |
| FDW008 | — | Method name contains underscore |
| FDW009 | — | Duplicate type name in compilation (disabled by default) |
| FDW010 | — | Implementation-specific type in base assembly (Info) |
| FDW011 | — | Service/Config/TypeOption with implementation prefix (Warning) |

## Setup

The analyzers are automatically applied to all FDW source projects via `Directory.Build.props`:

```xml
<ProjectReference Include="...\Fdw.Conventions.Analyzers.csproj"
                  OutputItemType="Analyzer"
                  ReferenceOutputAssembly="false" />
<ProjectReference Include="...\Fdw.Conventions.CodeFixes.csproj"
                  OutputItemType="Analyzer"
                  ReferenceOutputAssembly="false" />
```

Default thresholds are configured in `Directory.Build.props`:

```xml
<PropertyGroup>
  <FDW_MaxMethodLines>60</FDW_MaxMethodLines>
  <FDW_MaxCyclomaticComplexity>10</FDW_MaxCyclomaticComplexity>
  <FDW_DefaultStringComparison>Ordinal</FDW_DefaultStringComparison>
</PropertyGroup>
```

These properties are exposed to analyzers via `<CompilerVisibleProperty>`.

## Configuration Hierarchy

Thresholds for FDW006 and FDW007 are resolved in this order:

1. **Method-level attribute**: `[ConventionOverride(MaxMethodLines = 100)]` on the method
2. **Class-level attribute**: `[ConventionOverride(MaxCyclomaticComplexity = 20)]` on the class
3. **MSBuild property**: `<FDW_MaxMethodLines>80</FDW_MaxMethodLines>` in .csproj
4. **Solution default**: Values from `Directory.Build.props`
5. **Built-in default**: 60 lines / 10 complexity

### Per-Project Override

Add to the project's `.csproj`:

```xml
<PropertyGroup>
  <FDW_MaxMethodLines>100</FDW_MaxMethodLines>
</PropertyGroup>
```

### Per-Method/Class Override

```csharp
using Fdw.Conventions;

[ConventionOverride(MaxMethodLines = 120)]
public void LargeInitializationMethod()
{
    // This method is allowed up to 120 executable lines
}

[ConventionOverride(MaxCyclomaticComplexity = 25)]
public class CommandLineParser
{
    // All methods in this class can have up to 25 complexity
}
```

## Rule Details

### FDW005 - File Name Must Match Type Name

- **Severity**: Warning
- **Replaces**: MA0048
- **Improvement**: Supports generic arity variants (e.g., `Result<T>` and `Result<T,U>` in `Result.cs`)
- **Tool**: `dotnet fdw-split` — batch-splits multi-type files (see [CLI Tools](#cli-tools) below)

### FDW006 - Method Too Long

- **Severity**: Warning
- **Replaces**: MA0051
- **Default threshold**: 60 executable lines
- **Counts**: Executable statements only (excludes comments, blank lines, brace-only lines)
- **Skips**: Expression-bodied members, abstract/extern/partial methods

**When to use `[ConventionOverride]`**: For methods that are long but straightforward — sequential logging, linear initialization, repetitive-but-clear code. Don't extract methods just to satisfy a line count when it would reduce clarity.

### FDW007 - Method Too Complex

- **Severity**: Warning
- **Default threshold**: 10
- **Complexity sources**: `if`, `while`, `for`, `foreach`, `do`, `catch`, `case`, `?:`, `??`, `?.`, `&&`, `||`
- **Isolation**: Nested lambdas, anonymous methods, and local functions are NOT counted toward the parent method's complexity

**When to use `[ConventionOverride]`**: For methods that genuinely need branching (parsers, validators, state machines) where extracting strategies would add complexity rather than reduce it.

### FDW008 - Method Name Contains Underscore

- **Severity**: Warning
- **Excludes**: Test methods, test projects, P/Invoke, external overrides/implementations
- **Suggested fix**: Remove underscores, capitalize following characters (`Get_User_Name` -> `GetUserName`)

## Code Fixes

### Suppress with [ConventionOverride] (FDW006, FDW007)

Adds the `[ConventionOverride]` attribute with a threshold set above the current value plus a buffer:
- FDW006: actual line count + 20
- FDW007: actual complexity + 5

If the method already has `[ConventionOverride]`, the new property is merged into the existing attribute.

### Rename Method (FDW008)

Uses Roslyn's `Renamer.RenameSymbolAsync` to rename the method and update all references across the solution.

### Add StringComparison (MA0006)

Adds `StringComparison.{configured}` as the last argument to string methods. The comparison type is read from `FDW_DefaultStringComparison` MSBuild property (default: `Ordinal`).

Handles: `Equals`, `StartsWith`, `EndsWith`, `IndexOf`, `Contains`, `Compare`.

### Add ConfigureAwait(false) (AsyncFixer04)

Wraps `await expr` with `await expr.ConfigureAwait(false)`. Skips expressions that already have `.ConfigureAwait`.

## Severity Configuration

In `.editorconfig`:

```ini
dotnet_diagnostic.MA0048.severity = none     # Replaced by FDW005
dotnet_diagnostic.MA0051.severity = none     # Replaced by FDW006+FDW007
dotnet_diagnostic.FDW005.severity = warning
dotnet_diagnostic.FDW006.severity = warning
dotnet_diagnostic.FDW007.severity = warning
dotnet_diagnostic.FDW008.severity = warning
```

### FDW009 - Duplicate Type Name in Compilation

- **Severity**: Warning (disabled by default)
- **Purpose**: Flags public types with the same simple name in different namespaces within the same compilation
- **Skips**: Types with different generic arity (`None` and `None<T>` are not duplicates), TypeOption/TypeCollection types
- **Enable**: `dotnet_diagnostic.FDW009.severity = warning` in `.editorconfig`

### FDW010/FDW011 - Misplaced Implementation Type

- **FDW010 Severity**: Info
- **FDW011 Severity**: Warning
- **Scope**: Only applies to `*.Services.{Domain}.Abstractions` and `*.Services.{Domain}` assemblies
- **FDW010**: Any type with a non-domain implementation prefix (e.g., `EmailChannel` in `Notifications.Abstractions`)
- **FDW011**: TypeOption, Configuration, or Service type with a non-domain prefix (higher severity)

These detect implementation-specific types that belong in their own dedicated assembly. For example, `EmailChannel`, `EmailConfiguration`, and `TeamsChannel` in `Services.Notifications.Abstractions` should be in `Services.Notifications.Email` and `Services.Notifications.Teams`.

**Detection heuristics:**
- Extracts the first PascalCase word from type names as the "prefix"
- Compares against the domain name (e.g., "Notification" in `Services.Notifications`)
- Checks for sibling implementation assemblies (e.g., `Services.Notifications.Email`)
- Requires 3+ types with the same non-domain prefix (cluster detection)
- Excludes generic prefixes: Base, Abstract, Default, Internal, Invalid, Missing, Unknown, Unsupported

## Batch Fixing

Code fixes that modify existing files work with `dotnet format`:

```bash
# Fix underscore naming, string comparison, ConfigureAwait
dotnet format --diagnostics FDW008 MA0006 AsyncFixer04
```

Note: `dotnet format` cannot create new files. For FDW005 (splitting multi-type files), use the `fdw-split` CLI tool instead.

FDW006 and FDW007 code fixes add `[ConventionOverride]` rather than modifying code, so they are best applied selectively through IDE quick-fixes.

## CLI Tools

### fdw-split — File Splitter

Splits C# files containing multiple top-level type declarations into one file per type. This fixes FDW005 violations that `dotnet format` cannot handle (because `dotnet format` cannot create new documents).

**Install:**

```bash
dotnet tool install --global Fdw.Conventions.FileSplitter
```

**Usage:**

```bash
# Split a single file
dotnet fdw-split src/MyProject/Results/ResultCodes.cs

# Split all files in a project
dotnet fdw-split src/MyProject/MyProject.csproj

# Split all files in a directory
dotnet fdw-split src/MyProject/

# Preview changes without modifying files
dotnet fdw-split src/MyProject/ --dry-run
```

**Example output:**

```
  [create] WorkflowResultCodeBase -> Results/WorkflowResultCodeBase.cs
  [create] WorkflowNotFoundCode -> Results/WorkflowNotFoundCode.cs
  [create] WorkflowNameRequiredCode -> Results/WorkflowNameRequiredCode.cs
  [create] TagRequiredCode -> Results/TagRequiredCode.cs
Extracted 4 type(s) from Results/WorkflowResultCodes.cs
Created 4 new file(s) from 1 source file(s).
```

**Behavior:**
- Keeps the type matching the filename in the original file
- Creates new `{TypeName}.cs` files for all other types
- Copies namespace and using directives to each new file
- Merges into existing files if the target already exists
- Preserves XML doc comments, attributes, and pragma directives
- Skips nested types and generated files
- Excludes `obj/` and `bin/` directories
