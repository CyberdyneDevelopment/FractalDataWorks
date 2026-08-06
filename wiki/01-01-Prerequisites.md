# Prerequisites

## Required Software

- **.NET 10 SDK** - [Download](https://dotnet.microsoft.com/download)
- **IDE** - Visual Studio 2022, VS Code with C# extension, or JetBrains Rider

## Verify Installation

```bash
dotnet --version
# Expected: 10.0.x
```

## NuGet Packages

The FractalDataWorks packages are published to nuget.org:

- `Fdw.Abstractions` - Core abstractions (IGenericConfiguration, IGenericService)
- `Fdw.Collections` - TypeCollection base classes and interfaces
- `Fdw.Collections.SourceGenerators` - Source generators for TypeCollection O(1) lookups
- `Fdw.Registration.SourceGenerators` - Cross-assembly TypeOption registration via module initializers
- `Fdw.MessageLogging.Abstractions` - Logging abstractions
- `Fdw.MessageLogging.SourceGenerators` - Source generators for structured logging
- `Fdw.Results` - Result pattern implementation (IGenericResult)

## Next Steps

Continue to [Quick Start](01-02-Quick-Start.md) to build and run the Reference Solution.
