# Source Generator Template Installation & Usage

## Install the Template

### Option 1: Install from Directory (Local Development)

```bash
cd C:\development\WIP\FractalDataWorks
dotnet new install templates/SourceGeneratorSolution
```

### Option 2: Install from NuGet Package

```bash
# Pack the template first
cd templates/SourceGeneratorSolution/.template.config
nuget pack template.nuspec

# Install from package
dotnet new install Fdw.Templates.SourceGenerator.1.0.0.nupkg
```

### Option 3: Install from NuGet Feed

```bash
dotnet new install Fdw.Templates.SourceGenerator
```

## Verify Installation

```bash
dotnet new list sourcegen
```

You should see:
```
Template Name              Short Name  Language  Tags
-------------------------  ----------  --------  -----------------------
Source Generator Solution  sourcegen   [C#]      Source Generator/Roslyn
```

## Usage Examples

### Basic Usage

```bash
# Create with defaults (embeds in Abstractions)
dotnet new sourcegen -n MyFeature

# Creates:
# MyFeature/
# ├── MyFeature.Runtime/
# ├── MyFeature.SourceGenerators/
# └── MyFeature.Abstractions/  (with embedded generator)
```

### Custom Namespace

```bash
dotnet new sourcegen -n Transformations --namespace MyCompany
```

### Embed in Runtime Instead

```bash
dotnet new sourcegen -n MyFeature --embed-in Runtime
```

### No Embedding (Manual Setup)

```bash
dotnet new sourcegen -n MyFeature --embed-in Custom
```

### Without Shared Helpers

```bash
dotnet new sourcegen -n MyFeature --use-helpers false
```

### All Options

```bash
dotnet new sourcegen \
  --name MyFeature \
  --namespace MyCompany \
  --embed-in Abstractions \
  --use-helpers true \
  --enable-debug true \
  --frameworks "netstandard2.0;net8.0"
```

## CLI Parameters

| Parameter           | Short | Description                                      | Default              |
|---------------------|-------|--------------------------------------------------|----------------------|
| `--name`            | `-n`  | Feature name (required)                          | -                    |
| `--namespace`       | `-ns` | Root namespace                                   | FractalDataWorks     |
| `--embed-in`        | `-e`  | Where to embed generator (Runtime/Abstractions/Custom) | Abstractions   |
| `--use-helpers`     | `-h`  | Include shared helpers                           | true                 |
| `--enable-debug`    | `-d`  | Enable EmitCompilerGeneratedFiles                | true                 |
| `--frameworks`      | `-f`  | Target frameworks for Runtime                    | netstandard2.0;net10.0 |

## Visual Studio Integration

After installation, the template appears in:
- **File → New → Project**
- Search for "Source Generator"
- Shows wizard with all parameters

## Post-Creation Steps

### 1. Add to Solution

The template automatically tries to add projects to your solution. If it fails:

```bash
cd MyFeature
dotnet sln add **/*.csproj
```

Or manually:
```bash
dotnet sln add MyFeature.Runtime/MyFeature.Runtime.csproj
dotnet sln add MyFeature.SourceGenerators/MyFeature.SourceGenerators.csproj
dotnet sln add MyFeature.Abstractions/MyFeature.Abstractions.csproj
```

### 2. Build Generator

```bash
dotnet build MyFeature.SourceGenerators
```

### 3. Build Package

```bash
dotnet build MyFeature.Abstractions
# Package created at: artifacts/packages/MyFeature.Abstractions.*.nupkg
```

### 4. Use in Consumer Project

```xml
<ItemGroup>
  <!-- For local dev with ProjectReference -->
  <ProjectReference Include="..\MyFeature\MyFeature.Abstractions\MyFeature.Abstractions.csproj" />
  <ProjectReference Include="..\MyFeature\MyFeature.SourceGenerators\MyFeature.SourceGenerators.csproj"
                    OutputItemType="Analyzer"
                    ReferenceOutputAssembly="false" />
</ItemGroup>
```

Or:

```xml
<ItemGroup>
  <!-- From NuGet package (generator embedded automatically) -->
  <PackageReference Include="MyFeature.Abstractions" Version="1.0.0" />
</ItemGroup>
```

## Uninstall Template

```bash
dotnet new uninstall Fdw.Templates.SourceGenerator
```

Or from directory:
```bash
dotnet new uninstall C:\development\WIP\FractalDataWorks\templates\SourceGeneratorSolution
```

## Troubleshooting

### Template Not Found

```bash
# List all installed templates
dotnet new list

# Reinstall
dotnet new install templates/SourceGeneratorSolution --force
```

### Generator Not Running

1. Check `obj/generated` folder exists
2. Verify generator reference has `OutputItemType="Analyzer"`
3. Clean and rebuild: `dotnet clean && dotnet build`

### ILRepack Errors

Check `bin/Debug/netstandard2.0/ILRepack.log` for details.

## Examples

### Creating a Transformation Generator

```bash
dotnet new sourcegen -n Transformations -ns Acme.DataPipeline
cd Transformations

# Implement your generator logic
# Edit Transformations.SourceGenerators/Generators/TransformationsProviderGenerator.cs

# Build and test
dotnet build Transformations.SourceGenerators
dotnet build Transformations.Abstractions

# Use in a project
dotnet add reference ../Transformations/Transformations.Abstractions/Transformations.Abstractions.csproj
```

### Creating a Connection Provider Generator

```bash
dotnet new sourcegen -n Connections --embed-in Abstractions
cd Connections

# Your generator discovers [ConnectionOption] attributes and generates providers
# Package is ready to publish
dotnet pack Connections.Abstractions -c Release
```

## Tips

- Use `--dry-run` to preview without creating files
- Use `--output` to specify output directory
- Combine with git: `dotnet new sourcegen -n MyFeature && cd MyFeature && git init`
- Template respects `.gitignore` in parent directory

## Publishing Your Template

```bash
# Pack template
cd templates/SourceGeneratorSolution/.template.config
nuget pack template.nuspec

# Push to NuGet
nuget push Fdw.Templates.SourceGenerator.1.0.0.nupkg -Source https://api.nuget.org/v3/index.json

# Others can install
dotnet new install Fdw.Templates.SourceGenerator
```
