# Fdw.BindProof

A one-off proof, run once by the build owner (FDW-4784), not a job. `public/tools/versioning/bindcheck.py` models the runtime's rule that
a dll asks for each dependency at the assembly version it was built against and the runtime will not bind a lower one. This tool tests the
model with a real load: an OLD published `Fdw.Results` (which references `Fdw.Results.Abstractions`), the REBUILT `Fdw.Results.Abstractions`
from the new pack swapped in, and one object created across the boundary (`AuthCategory` derives from `ResultCategoryBase`).

The old package comes from nuget.org. On a machine whose user NuGet config maps `Fdw.*` to the internal feeds only, give the publish a
config of its own, in a scratch folder outside the repo (it needs no credentials): `<configuration><packageSources><clear /><add
key="nuget.org" value="https://api.nuget.org/v3/index.json" /></packageSources></configuration>`, passed as `--configfile <that file>` to
the publish below.

```bash
dotnet publish public/tools/Fdw.BindProof -c Release -p:OldVersion=1.5.4 --configfile <scratch nuget.config> -o proof
# swap in the rebuilt dependency (from the new pack, lib/netstandard2.0), keeping the old Fdw.Results:
cp <new pack>/lib/netstandard2.0/Fdw.Results.Abstractions.dll proof/
dotnet proof/Fdw.BindProof.dll --assembly Fdw.Results --type Fdw.Results.AuthCategory      # expect: BIND OK when the rebuilt dependency is not below what the old Fdw.Results asks for (its AssemblyVersion is at least the floor)
# control: the same dependency with a LOWER assembly version must be refused (build it with -p:AssemblyVersion=1.0.0.0):
cp <lower build>/Fdw.Results.Abstractions.dll proof/
dotnet proof/Fdw.BindProof.dll --assembly Fdw.Results --type Fdw.Results.AuthCategory      # expect: BIND FAILED: FileLoadException ...
```

If the control also binds, the runtime is more lenient than `bindcheck.py` models; the static check is then stricter than needed, and that
is reported, not hidden. `-p:OldVersion` is required; no version is assumed.
