# Fdw.UI.Web.Abstractions

Web-specific UI contracts.

Implementations live in the sibling packages; this one holds the interfaces, base types and models they agree on, so a consumer can depend on the contract without taking the implementation.

## Interfaces (4)

| Type | Kind | Purpose |
|---|---|---|
| `IComponentType` | interface | Interface for component types. |
| `IJavaScriptInterop` | interface | Generic JavaScript interop interface. Implementations vary by framework (Blazor, Node.js, browser). |
| `IRenderMode` | interface | Interface for render mode types. |
| `IWebComponent` | interface | Base interface for web components that can export metadata. |

## Base types (5)

| Type | Kind | Purpose |
|---|---|---|
| `ComponentTypeBase` | class | Base class for component types. |
| `ComponentTypes` | class | Collection of UI component types. |
| `RenderModes` | class | Collection of render modes for web components. |
| `WebComponentBase<TSelf, TModel>` | class | CRTP-based web component that can render to ANY JavaScript framework. Exports metadata that can be… |
| `WebRenderModeBase` | class | Base class for render mode types. |

## Models and supporting types (21)

| Type | Kind | Purpose |
|---|---|---|
| `BothWebRenderMode` | class | Show view and edit simultaneously render mode. |
| `CheckboxListComponentType` | class | Checkbox list component type. |
| `CollectionComponentType` | class | Nested collection component type. |
| `ComponentMetadata` | class | Framework-agnostic component metadata that can be serialized to JSON. Consumed by ANY JavaScript… |
| `CreateWebRenderMode` | class | Create new instance render mode. |
| `DatePickerComponentType` | class | Date selection picker component type. |
| `DateTimePickerComponentType` | class | Date and time selection picker component type. |
| `DropdownComponentType` | class | Single selection dropdown component type. |
| `EditWebRenderMode` | class | Editable input render mode. |
| `FileUploadComponentType` | class | File upload component type. |
| `HtmlRenderer` | class | Renders component metadata to HTML string. |
| `JsonEditorComponentType` | class | JSON structure editor component type. |

## Installation

```bash
dotnet add package Fdw.UI.Web.Abstractions --prerelease
```

## Dependencies

`Fdw.Collections` · `Fdw.UI.Abstractions`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
