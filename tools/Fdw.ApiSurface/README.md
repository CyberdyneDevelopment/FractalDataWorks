# Fdw.ApiSurface

Writes the public surface of a package, or of one assembly, as sorted text records. `public/tools/versioning/apisurface.py`
compares two such files and decides the lowest level (patch, minor or major) the change needs. This tool only lists what is
public; every rule is in the Python, which has the tests.

```bash
dotnet run --project public/tools/Fdw.ApiSurface -- --package Fdw.Results.1.5.2.nupkg --out old.txt
dotnet run --project public/tools/Fdw.ApiSurface -- --assembly bin/Release/net10.0/Fdw.Results.dll --tfm net10.0 --out new.txt
python3 public/tools/versioning/apisurface.py diff --old old.txt --new new.txt --settings public/tools/versioning/apisurface-settings.json
```

Records are `<tfm> TAB <kind> TAB <key> TAB <detail>`; the format is described at the top of `apisurface.py`. From a package it reads
`ref/<framework>` where there is one, otherwise `lib/<framework>`. It does not load the assemblies, so a package for another runtime
reads the same. It lists public and protected types and members (protected only on a type that can be inherited), their base type,
interfaces and generic constraints, constant values, and for each member the words an implementer or caller depends on: abstract,
virtual, override, static, required, init, out and optional parameters.

Not in the surface, on purpose: attributes (including nullability annotations), parameter names, and the assembly a type comes from.
A generator package has nothing under `lib`; its surface is its output, so it is levelled by the consumers it changes
(plan FDW-4784).
