# Domain: UI

## Purpose

The UI domain provides a **headless component architecture** for FractalDataWorks management interfaces. Components contain all business logic and state management. Skins provide visual rendering with zero logic.

## Sub-Domains

| Sub-Domain | Projects | Description |
|------------|----------|-------------|
| **UI.Components** | 7 | Headless Blazor components, TUI, RazorConsole |
| **UI.Skins** | 3 | MudBlazor, Tailwind, Authentication renderers |
| **UI.Services** | 12 | Providers, theme engine, UI-specific clients |

## Key Types

- **Headless Components** (`UI.Components.Blazor`) -- Contain state, validation, API calls. Expose `RenderFragment<TContext>` for rendering.
- **Providers** (`UI.Services`) -- State management layer between components and API clients (e.g., `ConnectionProvider`, `DataSetProvider`).
- **Skins** (`UI.Blazor.MudBlazor`, `UI.Blazor.Tailwind`) -- Pure rendering. Map `RenderFragment<TContext>` to framework-specific markup.
- **Theme Engine** (`UI.Themes`) -- HSL-based tenant theming with CSS variable generation.

## Patterns

### Headless Component Pattern

Components define behavior; skins define appearance:

```csharp
// Headless component -- all logic here
public class FdwConnectionList : ComponentBase
{
    [Parameter] public RenderFragment<ConnectionListContext> ChildContent { get; set; }

    // State, loading, error handling, API calls
    protected ConnectionListContext Context => new(Connections, IsLoading, OnEdit, OnDelete);
}

// Skin -- pure rendering, zero logic
<FdwConnectionList>
    <MudTable Items="@context.Connections" Loading="@context.IsLoading">
        ...
    </MudTable>
</FdwConnectionList>
```

### Provider Pattern (UI State Management)

Providers manage state between components and API clients:

```csharp
public class ConnectionProvider
{
    private readonly IConnectionClient _client;

    public IReadOnlyList<ConnectionDto> Connections { get; private set; }
    public bool IsLoading { get; private set; }

    public async Task LoadAsync() { ... }
    public async Task CreateAsync(CreateConnectionRequest request) { ... }
}
```

Each provider has its own MessageLog class (e.g., `ConnectionProviderLog`) with allocated EventId ranges.

### RenderFragment<TContext> Contract

The context object is the API contract between headless component and skin:

```csharp
public record ConnectionListContext(
    IReadOnlyList<ConnectionDto> Connections,
    bool IsLoading,
    Func<ConnectionDto, Task> OnEdit,
    Func<ConnectionDto, Task> OnDelete);
```

### Theme Engine

Themes use HSL tokens that map to CSS variables:

```csharp
// FDW CSS variables
theme.ToCssRootBlock()      // --fdw-primary-h, --fdw-primary-s, etc.

// Scalar API docs variables
theme.ToScalarCssBlock()    // --scalar-color-1, etc.
```

## Rules

1. **No logic in skins.** Skins receive context via `RenderFragment<TContext>` and render only. Event handlers, validation, API calls, state transitions -- all in the headless component.
2. **One provider per domain.** Each service domain (Connections, DataSets, DataStores, etc.) has exactly one provider class.
3. **Provider-specific MessageLog classes.** Use `ConnectionProviderLog`, `DataSetProviderLog`, etc. -- never generic `UiProviderLog`.
4. **API clients must be un-sealed with virtual methods** for testability (Moq requires this).
5. **Moq is the UI test framework.** Use `Mock<T>`, `.Setup().ReturnsAsync()`, `.Verify()`. Not NSubstitute.
6. **Components must not reference specific skin packages.** A component in `UI.Components.Blazor` must not import `MudBlazor`.
7. **Theme tokens are HSL-based.** Never use hex or RGB values directly. Use `ColorHelper` for conversions.

## Testing

- **Framework:** xUnit v3 + bunit 1.37.7 + Moq 4.20.72 + Shouldly 4.3.0
- **Pattern:** Provider tests mock API clients, verify state transitions and API calls
- **Traits:** `[Trait("Category", "Ui")]`, Priority P1/P2
- **5 test suites** (63 tests): ConnectionProvider, DataSetProvider, ConfigurationProvider, DataStoreProvider, RoleProvider

## Related Domains

- **Services** -- UI providers call service clients (e.g., `IConnectionClient`)
- **Web** -- `Web.Clients.Abstractions` defines the DTO contracts UI providers consume
- **Configuration** -- Config UI components display ManagedConfiguration data
- **Schema** -- Schema discovery UI for DataSet wizard
