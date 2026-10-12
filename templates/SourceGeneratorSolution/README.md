# Source Generator Solution Template

This template creates a complete source generator solution with three projects:

1. **Runtime** - Runtime types (attributes, base classes, interfaces)
2. **SourceGenerators** - Source generator with ILRepack configuration
3. **Package** (defaults to "Abstractions") - Consumer-facing package with embedded generator

> **Note**: The "Abstractions" naming is just a convention. Rename to whatever fits your architecture:
> - `MyFeature.Core` - For core functionality packages
> - `MyFeature.Client` - For client libraries
> - `MyFeature` - For single-package solutions
> - `MyFeature.Abstractions` - When following clean architecture

## Usage

### 1. Copy and Rename

```bash
# Copy the template
cp -r templates/SourceGeneratorSolution MyFeature

# Rename directories
mv MyFeature/__Name__.Runtime MyFeature/MyFeature.Runtime
mv MyFeature/__Name__.SourceGenerators MyFeature/MyFeature.SourceGenerators
mv MyFeature/__Name__.Abstractions MyFeature/MyFeature.Abstractions
```

### 2. Replace Placeholders

Replace these placeholders in all files:
- `__Name__` → Your feature name (e.g., `Connections`, `Transformations`)
- `__RootNamespace__` → Your root namespace (e.g., `FractalDataWorks`)

```bash
# PowerShell example
Get-ChildItem -Recurse -File | ForEach-Object {
    (Get-Content $_.FullName) -replace '__Name__', 'MyFeature' -replace '__RootNamespace__', 'FractalDataWorks' | Set-Content $_.FullName
}
```

### 3. Rename Files

Rename all files containing `__Name__`:
```bash
# Rename .csproj files
mv MyFeature.Runtime/__Name__.Runtime.csproj MyFeature.Runtime/MyFeature.Runtime.csproj
mv MyFeature.SourceGenerators/__Name__.SourceGenerators.csproj MyFeature.SourceGenerators/MyFeature.SourceGenerators.csproj
mv MyFeature.Abstractions/__Name__.Abstractions.csproj MyFeature.Abstractions/MyFeature.Abstractions.csproj

# Rename source files
mv MyFeature.Runtime/Attributes/__Name__OptionAttribute.cs MyFeature.Runtime/Attributes/MyFeatureOptionAttribute.cs
mv MyFeature.Runtime/__Name__Base.cs MyFeature.Runtime/MyFeatureBase.cs
mv MyFeature.SourceGenerators/Generators/__Name__ProviderGenerator.cs MyFeature.SourceGenerators/Generators/MyFeatureProviderGenerator.cs
mv MyFeature.Abstractions/I__Name__.cs MyFeature.Abstractions/IMyFeature.cs
```

### 4. Add to Solution

```bash
dotnet sln add MyFeature/MyFeature.Runtime/MyFeature.Runtime.csproj
dotnet sln add MyFeature/MyFeature.SourceGenerators/MyFeature.SourceGenerators.csproj
dotnet sln add MyFeature/MyFeature.Abstractions/MyFeature.Abstractions.csproj
```

### 5. Build

```bash
dotnet build MyFeature/MyFeature.SourceGenerators
dotnet build MyFeature/MyFeature.Abstractions
```

## Project Structure

```
MyFeature/
├── MyFeature.Runtime/           # Runtime types
│   ├── Attributes/
│   │   └── MyFeatureOptionAttribute.cs
│   ├── MyFeatureBase.cs
│   └── MyFeature.Runtime.csproj
│
├── MyFeature.SourceGenerators/       # Generator implementation
│   ├── Generators/
│   │   └── MyFeatureProviderGenerator.cs
│   ├── ILRepack.targets
│   └── MyFeature.SourceGenerators.csproj
│
└── MyFeature.Abstractions/           # Public package
    ├── IMyFeature.cs
    └── MyFeature.Abstractions.csproj
```

## How It Works

### Local Development (ProjectReferences)

Projects referencing `MyFeature.Abstractions` via `<ProjectReference>` get:
- Runtime types from Runtime (transitive)
- Generator runs via ProjectReference with `OutputItemType="Analyzer"`

Consumer projects need to explicitly reference the generator:
```xml
<ProjectReference Include="path\to\MyFeature.SourceGenerators\..."
                  OutputItemType="Analyzer"
                  ReferenceOutputAssembly="false" />
```

### Package Scenario (NuGet)

When `MyFeature.Abstractions` is published as a NuGet package:
- Runtime types included in `lib/netstandard2.0`
- Generator embedded in `analyzers/dotnet/cs`
- Consumers get generator automatically - no manual reference needed

```xml
<PackageReference Include="MyFeature.Abstractions" Version="1.0.0" />
```

## Key Features

✅ **ILRepack Integration** - Merges generator dependencies into single DLL
✅ **Automatic Packaging** - `GeneratePackageOnBuild=true` creates packages on build
✅ **Local Package Feed** - Outputs to `artifacts/packages` for testing
✅ **Debug Support** - `EmitCompilerGeneratedFiles` enabled for viewing generated code
✅ **Multi-Targeting** - Runtime targets both netstandard2.0 and net10.0

## Customization

### Add Dependencies to Runtime

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" />
</ItemGroup>
```

### Add Generator Helpers

Update SourceGenerators references:
```xml
<ProjectReference Include="..\..\src\Fdw.SourceGenerators\..." />
<ProjectReference Include="..\..\src\Fdw.CodeBuilder.CSharp\..." />
```

### Implement Generator Logic

Edit `Generators/__Name__ProviderGenerator.cs` to generate your code.

## Troubleshooting

### Generator Not Running

1. Clean and rebuild: `dotnet clean && dotnet build`
2. Check `obj/generated` folder exists
3. Verify generator reference has `OutputItemType="Analyzer"`

### ILRepack Errors

1. Check all referenced assemblies exist
2. Verify `ILRepack.targets` is in generator project root
3. Check ILRepack log at `bin/Debug/netstandard2.0/ILRepack.log`

### Package Not Found

1. Build Abstractions project to create package
2. Check `artifacts/packages` exists
3. Verify `NuGet.config` includes local feed:
```xml
<packageSources>
  <add key="local-dev" value="artifacts/packages" />
</packageSources>
```
