# Fdw.Services.Descriptions.Abstractions

The contracts of the descriptions domain: the description types, the description sub-types, the key that names one, and the service that saves, deletes and reads it.

## Contracts

| Type | Shape | Purpose |
|---|---|---|
| `IDescriptionService` | interface | Saves, deletes and reads a description; reads every description of one thing; names the language of today's text. |
| `IDescriptionType` | interface | A description type: a thing that has a description, with the permission that reads a thing of it and whether it belongs to an owner that names it (a field's data set). |
| `IDescriptionSubType` | interface | A description sub-type: a text about a thing. Part of a description's key. |

## Collections

| Type | Shape | Purpose |
|---|---|---|
| `DescriptionTypes` | TypeCollection | The description types. A package that owns a described thing declares one `[TypeOption(typeof(DescriptionTypes), "...")]`; the descriptions domain needs no change. |
| `DescriptionSubTypes` | TypeCollection | The description sub-types. One ships, `Text`: the text of a thing's description (a sub-type cannot be named `Description`, which hides a member every type option has). |

## Types

| Type | Shape | Purpose |
|---|---|---|
| `DescriptionKey` | class | Type, sub-type, thing Id and language. Nothing in it is defaulted. |
| `DescriptionOwner` | class | The owner of a child thing (a field's data set): its logical Id and name. |
| `DescriptionEntry` | class | A key, the thing's name, its owner and the text, as a read returns it. |
| `DataSetDescriptionType`, `DataSetFieldDescriptionType` | options | The two types the platform ships. Both are read by whoever holds `datasets:read`. |

## Dependencies

`Fdw.Abstractions` · `Fdw.Collections` · `Fdw.Types.Abstractions`

Build-time only: `Fdw.Collections.SourceGenerators`
