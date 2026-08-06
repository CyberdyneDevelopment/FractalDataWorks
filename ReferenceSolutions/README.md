# Reference Solutions

The FractalDataWorks reference solutions live in their own repositories, so each one builds,
deploys and versions independently and consumes FDW the way any other consumer does — as NuGet
packages, with no `InternalsVisibleTo` back into the framework.

That last part is deliberate: the reference solutions are third-party consumers on purpose.
Anything needed to compose an application on FDW has to be public API, and these repositories are
what prove it.

## Repositories

| Solution | Repository | Runs on |
|----------|-----------|---------|
| **ApiSolution** | [reference/reference-api](https://gitlab.cyberdynedevelopment.dev/reference/reference-api) | `:5020` |
| **EtlServer** | [reference/reference-etl](https://gitlab.cyberdynedevelopment.dev/reference/reference-etl) | `:5022` |
| **SchedulerServer** | [reference/reference-scheduler](https://gitlab.cyberdynedevelopment.dev/reference/reference-scheduler) | `:5024` |
| **ManagementUI** | [reference/reference-ui](https://gitlab.cyberdynedevelopment.dev/reference/reference-ui) | `:5026` |

The `ManagementUI-Tailwind` and `ManagementUI-WASM` directories were variants of the same
management UI and moved into `reference-ui` with it.

Each repository is independently buildable with `dotnet build`. The subdirectories here are stubs
that record where each solution went; they hold no source.

## Reference service types

The connection aggregations these solutions use are not in this repository either. A connection
kind's aggregation and its service-type surface ship from `reference-servicetypes` as a pair of
packages — `ReferenceConnections.<Kind>` for the composition, and
`ReferenceConnections.<Kind>.ServiceType` for the registration that enlists it. The framework
keeps the components those aggregations compose: dialects, translators, converters, native types,
error handlers and limits.
