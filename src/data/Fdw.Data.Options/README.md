# Fdw.Data.Options

Data options: the kinds of option a person with the right permission can turn on or off for a value in a column of a data container, and what applying each one does.

FDW supplies the kinds of option and what applying them does. It does not own the table that holds the stored options; `DataOptionRowFields` names the fields a reader expects (`DataStore`, `Container`, `Column`, `Value`, `OptionType`, `Option`, `Active`).

## Resolve an option from stored text

```csharp
var option = DataOptions.ByName(row.OptionType).ByName(row.Option);
var result = option.Apply(new DataOptionContext(
    dataStore, container, column, row.OptionType, row.Option, row.Value, row.IsActive, valuesSoFar, logger));
```

An unknown option type or option gives the collection's NotFound sentinel, whose `Apply` returns a failure (`DATAOPTION-31000` and `DATAOPTION-30000`); nothing throws. A row that needs a value and has none fails `DATAOPTION-20000`.

## Shape

- `DataOptions` is the root collection of option types. A member is a dispatcher (`ByName(optionName)`, `Options`), like `DefaultValues` in `Fdw.Data.Abstractions`.
- `SelectList` is the first option type. Its child collection `SelectListOptions` has `Include` (puts the value in the list) and `Exclude` (takes it out).
- `Apply` returns `IGenericResult<DataOptionOutcome>`. `DataOptionOutcome` is not generic on its type, because a caller holds only strings read from rows; `SelectListOutcome` carries the resulting list.
- An inactive row changes nothing. Values compare exactly (ordinal) and are never listed twice. Exclude applies after Include (`IDataOption.ApplyOrder`), so the list does not depend on the order the rows were read in.

**Registration.** `SelectList` joins `DataOptions` from `Fdw.Data.Options`, a different assembly from the root, so the registration is written into the executable that references both, by the `Fdw.Registration.SourceGenerators` analyzer (`OutputItemType="Analyzer"`), as it is for every cross-assembly type option. A host or test executable without that generator finds `DataOptions.ByName("SelectList")` is the NotFound sentinel, and `Apply` fails with `DATAOPTION-31000`; it is a loud failure, never a default. Library projects need nothing.

Another package adds an option type with `[TypeOption(typeof(DataOptions), "Name")]`, or an option of SelectList with `[TypeOption(typeof(SelectListOptions), "Name")]`.

The `ReadDataOptions` pipeline step (`Fdw.Services.Etl`) reads the stored rows for a column and keeps the resulting list in a variable.
