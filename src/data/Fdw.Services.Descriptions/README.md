# Fdw.Services.Descriptions

The descriptions domain: one table that holds every description, keyed by description type, sub-type, the thing's logical Id and a language.

From 1.6.0 (FDW-6208) the table is where a moved domain's descriptions live: the domain's own `Description` column is gone, its record keeps a `[NotStored]` `Description` member, and its provider writes the table in the unit of its save and fills the member on every read. Moved so far: the data sets and their fields, tags and workspaces. Nothing else writes the table.

## Tables (ConfigurationDb, schema `catalog`)

| Table | Holds |
|---|---|
| `catalog.Description` | The domain row: Id, Name, Domain, Implementation, the key (`DescriptionType`, `DescriptionSubType`, `ThingId`, `Language`, `Ordinal`) and the names a search shows (`ThingName`, and `OwnerName` and `OwnerId` for a child such as a field). Name is the key as text, `Type/SubType/ThingId/Language/Ordinal`. |
| `catalog.DescriptionConfiguration` | The implementation row: `Text`, never empty. |

## Parts (FDW-6123)

A description is one or more parts. A part is a whole `catalog.Description` row with its own `catalog.DescriptionConfiguration` row; `Ordinal` (from 1, no database default, part of the key) says its place in reading order. A text within the part length is one row, Ordinal 1. A longer one is split by `DescriptionParts.Split` and every read joins the parts back in Ordinal order, so a caller of `IDescriptionReader` or a filled `Description` member never sees a part and the text read is the text written, character for character.

- **Where a cut falls.** Before a whitespace run, so a word is not broken, and the whitespace starts the next part. A run longer than the part length with no whitespace is cut at the part length, in the middle of the run. No part is whitespace alone (the database refuses that), so whitespace at the end of the text stays on the last part.
- **A write replaces all the parts in one save**, in one unit: it writes parts 1 to n and removes every part above n the description had, so fewer parts leave no stale higher Ordinal.
- **A read is one query for a page.** `ReadMany`, `ReadOwnedBy` and `Fill` each read every part of every description asked for in one query and join in memory; a part is never a query of its own. Parts that are not 1 to n with no gap fail with 91400 `DescriptionPartsBroken`; a text with a part missing is never answered.
- **A search lists a description once.** `Find` searches every part, name a description once by its first part with the parts joined, and page and count descriptions, never parts. The limit: a phrase that crosses the end of one part and the start of the next is in neither part, so it is not found; a word within a part is.
- **The part length is configuration**: `DescriptionServiceTypes.PartLength`, 4000 unless a host sets another before `Register` runs; a value below 1 stops the host (60344). Nothing in code caps it, and the text column is `VARCHAR(MAX)`. A change applies to what is written from then on: a description already stored keeps the parts it has until it is next saved, and reads join either way.

## Services

| Type | Shape | Purpose |
|---|---|---|
| `DescriptionService` | class | `Save(key, thingName, owner, text)`, `Delete` and `Language`; each description is stored as one or more parts and read joined by `IDescriptionReader`. Refuses a missing type, sub-type, thing, language, thing name, text, or an owner's name that the type does (not) take, each with its own coded message. |
| `DescriptionConfigurationProvider` | provider | The domain provider over `catalog.Description`: version-on-write saves, tag-based cache invalidation, paged reads of the domain rows. |
| `DescriptionImplementationConfigurationProvider` | provider | The implementation provider over `catalog.DescriptionConfiguration`. |
| `IDescriptionWriter` | contract | `Save`, `Delete`, `DeleteAbout`, `DeleteOwnedBy`, `DeleteOwnedExcept`, `Describe` (saves a thing's text in the language of today, or removes its description when the text is empty) and `Language`, inside the unit of work an owning provider opened, so the description commits or rolls back with the write of the thing it describes. The same class implements `IDescriptionService`. |
| `DescriptionServiceTypes`, `DefaultDescriptionServiceType` | registration | The `[ServiceTypeCollection]` and its `[Implementation]` that register the providers and the service in their Register phase. |

## Filling

The owning domain's provider writes the table in the unit of its own write. It declares `IConfigurationWriteObserver<T>` (`Fdw.Services`), which `DomainConfigurationProviderBase` calls after a member is written and before the unit commits, and `IReferencedConfigurationProvider<T>`, which a delete calls before it retires the member. `DataSetConfigurationProvider` (`Fdw.Services.Data`) does both for every data set type and its fields; `TagConfigurationProvider` and `WorkspaceConfigurationProvider` do both with one `Describe` and one `DeleteAbout`. On a read the provider declares `IConfigurationReadObserver<T>` (or `IConfigurationDomainReadObserver<T>` for a page of domain records or a view) and fills the set once with `IDescriptionReader.Fill` or `ReadMany`; a view never carries a description, because the text is in the language of the caller's tenant. A text that is empty writes no row and removes the one the thing had. A host that registers data sets registers this domain with it and declares the two containers in its `configurationSchema.json`. Seed 65 (databases-seed) backfills the existing descriptions.

## Language

The language of today's text is the server setting `DescriptionLanguage` (`SettingDefinitions.DescriptionLanguage`), seeded in databases-seed and resolved server, then tenant. When it resolves to nothing, `IDescriptionService.Language()` fails with result code 61400 `DescriptionLanguageNotStated`; no language is assumed. `Save` and `Delete` take the language in their key.

## Result codes (prefix `Descriptions`)

21400 `DescriptionTypeUnknown` · 21401 `DescriptionSubTypeUnknown` · 21402 `DescriptionThingMissing` · 21403 `DescriptionLanguageMissing` · 21404 `DescriptionTextMissing` · 21405 `DescriptionThingNameMissing` · 21406 `DescriptionOwnerMissing` · 21407 `DescriptionOwnerNotExpected` · 61400 `DescriptionLanguageNotStated` · 91400 `DescriptionPartsBroken`. Logged at start-up: 60344 `PartLengthInvalid`.

## Dependencies

`Fdw.Services.Descriptions.Abstractions` · `Fdw.Services` · `Fdw.Services.Data.Abstractions` · `Fdw.Services.Settings` · `Fdw.Services.Authentication.Abstractions` · `Fdw.Commands.Data` · `Fdw.Data`

Build-time only: `Fdw.Collections.SourceGenerators`, `Fdw.Configuration.SourceGenerators`, `Fdw.Data.SourceGenerators`, `Fdw.MessageLogging.SourceGenerators`
