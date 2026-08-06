# FractalDataWorks Message Logger Template

This template creates a static logger class that uses source generation to create high-performance logging methods that return `IGenericMessage`.

## Installation

From the templates directory:

```bash
dotnet new install .
```

Or install from a NuGet package (if packaged):

```bash
dotnet new install Fdw.Templates
```

## Usage

### Using dotnet CLI

```bash
# Create a logger with default name "ServiceLogger"
dotnet new fdw-logger

# Create a logger with custom name
dotnet new fdw-logger --loggerName DatabaseLogger

# Create without examples
dotnet new fdw-logger --loggerName ApiLogger --includeExamples false

# Specify namespace
dotnet new fdw-logger --namespace MyCompany.Services --loggerName PaymentLogger
```

### Using Visual Studio

1. Right-click on your project
2. Select "Add" → "New Item"
3. Search for "FractalDataWorks Message Logger"
4. Enter the name (e.g., "DatabaseLogger")
5. Click "Add"

## Template Parameters

- **loggerName**: The name of the logger class (default: `ServiceLogger`)
- **namespace**: The namespace for the logger (default: `Fdw.Services`)
- **includeExamples**: Include example logging methods (default: `true`)

## Usage in Code

```csharp
using Microsoft.Extensions.Logging;
using Fdw.Results;
using Fdw.Messages;

public class MyService
{
    private readonly ILogger<MyService> _logger;

    public MyService(ILogger<MyService> logger)
    {
        _logger = logger;
    }

    public IGenericResult<Connection> Connect(string server, int port)
    {
        try
        {
            // ... connection logic ...

            if (!connected)
            {
                // Logs the error AND returns IGenericMessage for Result
                return GenericResult<Connection>.Failure(
                    ServiceLogger.ConnectionFailed(_logger, server, port)
                );
            }

            return GenericResult<Connection>.Success(
                connection,
                ServiceLogger.ServiceStarted(_logger, server)
            );
        }
        catch (Exception ex)
        {
            return GenericResult<Connection>.Failure(
                ServiceLogger.UnexpectedError(_logger, ex, "Connect")
            );
        }
    }
}
```

## Best Practices

1. **One Logger per Service/Domain**: Create separate logger classes for different services or domains
   - `DatabaseLogger` for database operations
   - `ApiLogger` for API calls
   - `PaymentLogger` for payment processing

2. **Unique Event IDs**: Ensure Event IDs are unique within a logger class
   - Use ranges: 1000-1999 for errors, 2000-2999 for warnings, etc.

3. **Structured Logging**: Use parameters instead of string concatenation
   ```csharp
   // Good
   [MessageLogging(Message = "User {userId} logged in")]

   // Bad
   [MessageLogging(Message = "User " + userId + " logged in")]
   ```

4. **Exception Handling**: Always include Exception parameter for error messages
   ```csharp
   [MessageLogging(
       EventId = 1001,
       Level = LogLevel.Error,
       Message = "Failed to process {operation}")]
   public static partial IGenericMessage OperationFailed(
       ILogger logger,
       Exception exception,
       string operation);
   ```

5. **MessageSeverity vs LogLevel**:
   - `LogLevel` is for the logging infrastructure
   - `MessageSeverity` is for your `IGenericMessage` / `IGenericResult` system
   - Use `AutoMapSeverity = false` when you need different severities

## Source Generator Requirements

This template requires:
- `Fdw.MessageLogging.Abstractions` package
- `Fdw.MessageLogging.SourceGenerators` package
- Your project must have `<OutputType>Library</OutputType>` or `<OutputType>Exe</OutputType>`

The source generator will automatically create implementations of the partial methods at compile time.
