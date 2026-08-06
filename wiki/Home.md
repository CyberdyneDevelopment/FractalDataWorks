# FractalDataWorks Wiki

Welcome to the FractalDataWorks documentation. This wiki guides you through building data-intensive applications using the FractalDataWorks framework.

## Getting Started

1. [Prerequisites](01-01-Prerequisites.md) - What you need to get started
2. [Quick Start](01-02-Quick-Start.md) - Build your first FractalDataWorks application
3. [What You Built](01-03-What-You-Built.md) - Understanding the generated solution
4. [Local Package Development](01-04-Local-Package-Development.md) - Working with local NuGet packages

## Solution Structure

5. [Project Layout](02-01-Project-Layout.md) - How projects are organized
6. [Directory.Build.props](02-02-Directory-Build-Props.md) - Shared build configuration
7. [Package Management](02-03-Package-Management.md) - Central package version management
8. [Naming Conventions](02-04-Naming-Conventions.md) - Project and namespace conventions

## Configuration

9. [ManagedConfiguration](03-01-ManagedConfiguration.md) - Database-persisted, source-generated configuration
10. [Configuration Writers](03-02-ConfigurationWriters.md) - CQRS write-side for configuration persistence

## TypeCollections

10. [Overview](04-01-Overview.md) - Source-generated enhanced enums
11. [Base Classes](04-02-Base-Classes.md) - Defining base classes
12. [Concrete Options](04-03-Concrete-Options.md) - Creating individual options
13. [Collection Declaration](04-04-Collection-Declaration.md) - The TypeCollection attribute
14. [Generated Lookups](04-05-Generated-Lookups.md) - O(1) access methods
15. [Dispatcher Pattern](04-06-Dispatcher-Pattern.md) - Type-safe dispatch without reflection
16. [Components Guide](04-07-Components-Guide.md) - All 6 TypeCollection variants explained

## Data Access

17. [DataGateway Pattern](05-01-DataGateway-Pattern.md) - Unified data access layer
18. [DataSets](05-02-DataSets.md) - Logical data definitions

## Service Domains

19. [Service Domains Overview](06-01-Service-Domains-Overview.md) - Plugin architecture for services
20. [Creating a Service Domain](06-02-Creating-Service-Domain.md) - Step-by-step implementation guide
21. [Connections Service Domain](06-03-Connections-Service-Domain.md) - Example: Connections pattern

## Schema & Database

22. [Schema Abstractions](08-01-Schema-Abstractions.md) - Unified schema definitions
23. [Database Schema](08-02-Database-Schema.md) - ConfigurationDb structure (50 tables)

## Message Logging

21. [Overview](07-01-Overview.md) - Source-generated structured logging
22. [MessageLogging Attribute](07-02-MessageLogging-Attribute.md) - Defining message loggers
23. [Logger Classes](07-03-Logger-Classes.md) - Creating logger classes
24. [IGenericMessage](07-04-IGenericMessage.md) - Message interface
25. [Result Integration](07-05-Result-Integration.md) - Integrating with Results
26. [ResultCodes](07-06-ResultCodes.md) - Structured, type-safe error codes

## Reference Solution

All code examples in this wiki reference the [ReferenceSolution](../samples/ReferenceSolution) project. Build it to follow along:

```bash
# From the FractalDataWorks repository root
cd public/samples/ReferenceSolution
dotnet build
```
