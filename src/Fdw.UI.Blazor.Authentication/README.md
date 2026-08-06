# Fdw.UI.Blazor.Authentication

Headless Blazor authentication components — login and session, with the markup left to the host.

Headless Blazor components for this domain — 7 `.razor` component(s) plus their supporting types.

Components are headless: a provider owns the state and the calls, a context carries them, and the consumer supplies the markup through a `RenderFragment<TContext>`. Nothing here renders a fixed skin, so a host can present the same component in any visual language.

## Context, provider and log

| Type | Kind | Purpose |
|---|---|---|
| `ForgotPasswordContext` | class | Context provided to the Content render fragment of FdwForgotPassword. Exposes the identifier model,… |
| `LoginFormContext` | class | Context provided to the FormContent render fragment of FdwLoginForm. Exposes the form model, state, and… |
| `OidcForgotPasswordProvider` | class | Implements forgot-password for OIDC providers by returning a redirect URL to the provider's account… |

## Installation

```bash
dotnet add package Fdw.UI.Blazor.Authentication --prerelease
```

## Dependencies

`Fdw.MessageLogging.Abstractions` · `Fdw.Services.Authentication.Abstractions`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
