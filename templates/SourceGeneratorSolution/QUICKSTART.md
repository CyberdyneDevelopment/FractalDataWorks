# Quick Start

## Create a New Source Generator Project in 30 Seconds

### Option 1: PowerShell Script (Recommended)

```powershell
cd templates/SourceGeneratorSolution
.\New-SourceGeneratorProject.ps1 -Name "MyFeature" -OutputPath "..\..\src"
```

This creates:
- `src/MyFeature/MyFeature.Runtime/`
- `src/MyFeature/MyFeature.SourceGenerators/`
- `src/MyFeature/MyFeature.Abstractions/`

### Option 2: Manual

```bash
# Copy template
cp -r templates/SourceGeneratorSolution src/MyFeature

# Replace placeholders (PowerShell)
cd src/MyFeature
Get-ChildItem -Recurse -File | ForEach-Object {
    (Get-Content $_.FullName) -replace '__Name__', 'MyFeature' -replace '__RootNamespace__', 'FractalDataWorks' | Set-Content $_.FullName
}

# Rename everything
# ... (see README.md for full steps)
```

## Add to Solution

```bash
dotnet sln add src/MyFeature/MyFeature.Runtime/MyFeature.Runtime.csproj
dotnet sln add src/MyFeature/MyFeature.SourceGenerators/MyFeature.SourceGenerators.csproj
dotnet sln add src/MyFeature/MyFeature.Abstractions/MyFeature.Abstractions.csproj
```

## Build & Test

```bash
# Build generator (creates merged DLL with ILRepack)
dotnet build src/MyFeature/MyFeature.SourceGenerators

# Build abstractions (creates NuGet package with embedded generator)
dotnet build src/MyFeature/MyFeature.Abstractions

# Package is now at: artifacts/packages/MyFeature.Abstractions.*.nupkg
```

## Use in a Consumer Project

### Local Development

```xml
<ItemGroup>
  <!-- Reference abstractions for runtime types -->
  <ProjectReference Include="..\MyFeature\MyFeature.Abstractions\MyFeature.Abstractions.csproj" />

  <!-- Reference generator so it runs -->
  <ProjectReference Include="..\MyFeature\MyFeature.SourceGenerators\MyFeature.SourceGenerators.csproj"
                    OutputItemType="Analyzer"
                    ReferenceOutputAssembly="false" />
</ItemGroup>
```

### NuGet Package

```xml
<ItemGroup>
  <!-- Generator flows automatically from package -->
  <PackageReference Include="MyFeature.Abstractions" Version="1.0.0" />
</ItemGroup>
```

## Implement Your Generator

Edit `MyFeature.SourceGenerators/Generators/MyFeatureProviderGenerator.cs`:

```csharp
// Find decorated types
var decoratedTypes = compilation.SyntaxTrees
    .SelectMany(tree => tree.GetRoot().DescendantNodes())
    .OfType<ClassDeclarationSyntax>()
    .Where(c => HasMyFeatureOptionAttribute(c))
    .ToList();

// Generate code
var code = GenerateMyFeatureProvider(decoratedTypes);
context.AddSource("MyFeatureProvider.g.cs", code);
```

## View Generated Code

Generated files appear at:
```
obj/generated/MyFeature.SourceGenerators/MyFeature.SourceGenerators.MyFeatureProviderGenerator/
```

Enable in .csproj:
```xml
<EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
<CompilerGeneratedFilesOutputPath>$(BaseIntermediateOutputPath)generated</CompilerGeneratedFilesOutputPath>
```

## Debug the Generator

1. Set breakpoint in `MyFeatureProviderGenerator.cs`
2. In Visual Studio: Debug → Attach to Process → find `VBCSCompiler.exe`
3. Rebuild consumer project to trigger breakpoint

Or use the Roslyn debugger:
```xml
<IsRoslynComponent>true</IsRoslynComponent>
```

## That's It!

You now have a fully configured source generator solution with:
- ✅ ILRepack merging dependencies
- ✅ NuGet packaging with embedded analyzer
- ✅ Local development support
- ✅ Debug-friendly configuration
- ✅ Multi-targeting support

Happy generating! 🚀
