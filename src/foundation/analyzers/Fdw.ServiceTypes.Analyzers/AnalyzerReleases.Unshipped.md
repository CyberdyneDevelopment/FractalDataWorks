### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
FDW024 | Usage | Error | ServiceTypeCollectionPhaseMethodsAnalyzer - [ServiceTypeCollection]/[PlatformServiceProvider] class does not declare the static Configure/Register/Initialize phase methods the registration generator emits a method group for
FDW025 | Critical | Error | InstancePropertyAnalyzer - Static Instance property pattern is forbidden on TypeOption/ServiceType classes
FDW027 | Usage | Error | TypeOptionConstructorAnalyzer - [TypeOption] class missing a public parameterless constructor
SVCTYPE001 | ServiceTypes | Warning | TypeLookupNamingConflictAnalyzer - TypeLookup generates method that conflicts with collection member
FDW044 | Usage | Error | ServiceProviderInjectionAnalyzer - service-option service injects another service-option service directly instead of its IFdwServiceProvider<TService, TConfiguration>
FDW045 | Usage | Error | FactoryProviderInjectionAnalyzer - service factory injects an IFdwServiceProvider or IServiceScopeFactory through its constructor instead of receiving resolved values from its owning provider
FDW060 | Design | Disabled | EndpointConstructorAllowlistAnalyzer - an endpoint constructor takes something other than ILogger, a provider interface or a declared cross-cutting type (FDW-4641 A; ships disabled, Error when an .editorconfig turns it on)
FDW061 | Design | Disabled | RuntimeServiceLocatorAnalyzer - a runtime service locator (IServiceProvider/IServiceScopeFactory member, GetService/GetRequiredService/CreateScope, HttpContext.RequestServices, FastEndpoints Resolve) outside a hosted worker, a registration body, an sp => lambda or [ServiceLocatorAllowed("reason")]; also an empty reason (FDW-4641 B; ships disabled)
FDW062 | Design | Disabled | RegistrationSurgeryAnalyzer - BuildServiceProvider; Remove/RemoveAt/RemoveAll/Replace/Clear/Insert on an IServiceCollection; reading ServiceDescriptor.Implementation*; an Add* extension on IServiceCollection/IHostApplicationBuilder (FDW-4641 C; ships disabled)
STC001 | Design | Error | PhaseFuncCompositionAnalyzer - service type or service type collection composes onto a phase body it already owns via Append*/Prepend* instead of setting its own func with Configuration/Registration/Initialization
STC002 | Design | Error | PhaseFuncCompositionAnalyzer - a base class between ServiceTypeBase and the declared service type sets a phase func; wiring every option needs belongs in the collection's Register

### Removed Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
FDW033 | Usage | Warning | TypeOptionBaseAnalyzer - keyed on attribute names no framework type has (FDW-3511)
FDW034 | Design | Warning | TypeOptionBaseConstructorAnalyzer - keyed on attribute names no framework type has (FDW-3511)
