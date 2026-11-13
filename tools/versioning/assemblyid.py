#!/usr/bin/env python3
"""Read the identity of the assemblies in a package without a .NET tool (FDW-5272): what bindcheck.py compares.

  assemblyid.py identity --package FILE.nupkg --out FILE

For every runtime assembly of the package (every .dll under lib/<framework>/) it writes one DEF record, the assembly's own name and
AssemblyVersion, and one REF record for each assembly it references, the version it asks for. A record is four tab-separated fields,
sorted:

  <framework> DEF <assembly> <a.b.c.d>
  <framework> REF <assembly>-><referenced assembly> <a.b.c.d>

It reads the ECMA-335 metadata tables itself (the PE header, the CLI header, the #~ stream, the #Strings heap): only the Assembly and
AssemblyRef tables are used, but to find them it has to know the size of every table before them, so the whole row layout of tables 0x00
to 0x23 is spelled out below. A file that is not a managed assembly, a table it does not know, or a truncated image stops the run and names
the file: nothing is assumed. This replaces `Fdw.ApiSurface --identity` in the release job, so the job publishes and runs no .NET tool.
"""
import argparse
import struct
import sys
import zipfile

CLI_HEADER_DIRECTORY = 14

# Coded indexes: (tag bits, the tables a value can point into). A value is as wide as 2 bytes until the largest of those tables needs more.
CODED = {
    "TypeDefOrRef": (2, (0x02, 0x01, 0x1B)),
    "HasConstant": (2, (0x04, 0x08, 0x17)),
    "HasCustomAttribute": (5, (0x06, 0x04, 0x01, 0x02, 0x08, 0x09, 0x0A, 0x00, 0x0E, 0x17, 0x14, 0x11, 0x1A, 0x1B, 0x20, 0x23, 0x26, 0x27, 0x28, 0x2A, 0x2C, 0x2B)),
    "HasFieldMarshal": (1, (0x04, 0x08)),
    "HasDeclSecurity": (2, (0x02, 0x06, 0x20)),
    "MemberRefParent": (3, (0x02, 0x01, 0x1A, 0x06, 0x1B)),
    "HasSemantics": (1, (0x14, 0x17)),
    "MethodDefOrRef": (1, (0x06, 0x0A)),
    "MemberForwarded": (1, (0x04, 0x06)),
    "Implementation": (2, (0x26, 0x23, 0x27)),
    "CustomAttributeType": (3, (0x06, 0x0A)),
    "ResolutionScope": (2, (0x00, 0x1A, 0x23, 0x01)),
    "TypeOrMethodDef": (1, (0x02, 0x06)),
}

# The columns of each table: an int is a fixed width in bytes; "str", "guid" and "blob" are heap indexes; ("table", n) is an index into table n;
# ("coded", name) is a coded index.
STR, GUID, BLOB = "str", "guid", "blob"
TABLES = {
    0x00: [2, STR, GUID, GUID, GUID],
    0x01: [("coded", "ResolutionScope"), STR, STR],
    0x02: [4, STR, STR, ("coded", "TypeDefOrRef"), ("table", 0x04), ("table", 0x06)],
    0x03: [("table", 0x04)],
    0x04: [2, STR, BLOB],
    0x05: [("table", 0x06)],
    0x06: [4, 2, 2, STR, BLOB, ("table", 0x08)],
    0x07: [("table", 0x08)],
    0x08: [2, 2, STR],
    0x09: [("table", 0x02), ("coded", "TypeDefOrRef")],
    0x0A: [("coded", "MemberRefParent"), STR, BLOB],
    0x0B: [1, 1, ("coded", "HasConstant"), BLOB],
    0x0C: [("coded", "HasCustomAttribute"), ("coded", "CustomAttributeType"), BLOB],
    0x0D: [("coded", "HasFieldMarshal"), BLOB],
    0x0E: [2, ("coded", "HasDeclSecurity"), BLOB],
    0x0F: [2, 4, ("table", 0x02)],
    0x10: [4, ("table", 0x04)],
    0x11: [BLOB],
    0x12: [("table", 0x02), ("table", 0x14)],
    0x13: [("table", 0x14)],
    0x14: [2, STR, ("coded", "TypeDefOrRef")],
    0x15: [("table", 0x02), ("table", 0x17)],
    0x16: [("table", 0x17)],
    0x17: [2, STR, BLOB],
    0x18: [2, ("table", 0x06), ("coded", "HasSemantics")],
    0x19: [("table", 0x02), ("coded", "MethodDefOrRef"), ("coded", "MethodDefOrRef")],
    0x1A: [STR],
    0x1B: [BLOB],
    0x1C: [2, ("coded", "MemberForwarded"), STR, ("table", 0x1A)],
    0x1D: [4, ("table", 0x04)],
    0x1E: [4, 4],
    0x1F: [4],
    0x20: [4, 2, 2, 2, 2, 4, BLOB, STR, STR],
    0x21: [4],
    0x22: [4, 4, 4],
    0x23: [2, 2, 2, 2, 4, BLOB, STR, STR, BLOB],
}
ASSEMBLY, ASSEMBLY_REF = 0x20, 0x23


class IdentityError(Exception):
    pass


def _u(data, offset, size, what):
    if offset < 0 or offset + size > len(data):
        raise IdentityError(f"the image ends before {what} (offset {offset})")
    return int.from_bytes(data[offset:offset + size], "little")


def _rva_to_offset(data, sections, rva, what):
    for virtual_address, virtual_size, raw_size, raw_pointer in sections:
        if virtual_address <= rva < virtual_address + max(virtual_size, raw_size):
            return raw_pointer + (rva - virtual_address)
    raise IdentityError(f"{what} (RVA {rva:#x}) is in no section")


def _sections(data, table_start, count):
    out = []
    for i in range(count):
        o = table_start + 40 * i
        out.append((_u(data, o + 12, 4, "a section header"), _u(data, o + 8, 4, "a section header"), _u(data, o + 16, 4, "a section header"), _u(data, o + 20, 4, "a section header")))
    return out


def _metadata(data):
    """(metadata root offset, size): where the CLI header says the metadata is."""
    if data[:2] != b"MZ":
        raise IdentityError("not a PE image (no MZ header)")
    pe = _u(data, 0x3C, 4, "the PE header offset")
    if data[pe:pe + 4] != b"PE\0\0":
        raise IdentityError("not a PE image (no PE signature)")
    sections_count = _u(data, pe + 6, 2, "the section count")
    optional_size = _u(data, pe + 20, 2, "the optional header size")
    optional = pe + 24
    magic = _u(data, optional, 2, "the optional header")
    if magic not in (0x10B, 0x20B):
        raise IdentityError(f"unknown optional header magic {magic:#x}")
    directories = optional + (96 if magic == 0x10B else 112)
    sections = _sections(data, optional + optional_size, sections_count)
    cli_rva, cli_size = _u(data, directories + 8 * CLI_HEADER_DIRECTORY, 4, "the CLI header directory"), _u(data, directories + 8 * CLI_HEADER_DIRECTORY + 4, 4, "the CLI header directory")
    if cli_rva == 0 or cli_size == 0:
        raise IdentityError("not a managed assembly (no CLI header)")
    cli = _rva_to_offset(data, sections, cli_rva, "the CLI header")
    metadata_rva, metadata_size = _u(data, cli + 8, 4, "the CLI header"), _u(data, cli + 12, 4, "the CLI header")
    return _rva_to_offset(data, sections, metadata_rva, "the metadata"), metadata_size


def _streams(data, root):
    if _u(data, root, 4, "the metadata root") != 0x424A5342:
        raise IdentityError("the metadata root has no BSJB signature")
    length = _u(data, root + 12, 4, "the metadata version length")
    count = _u(data, root + 16 + length + 2, 2, "the stream count")
    cursor, streams = root + 16 + length + 4, {}
    for _ in range(count):
        offset, size = _u(data, cursor, 4, "a stream header"), _u(data, cursor + 4, 4, "a stream header")
        end = data.index(b"\0", cursor + 8)
        streams[data[cursor + 8:end].decode("ascii")] = (root + offset, size)
        cursor = (end + 1 + 3) & ~3
    return streams


def _string(data, heap_start, index):
    start = heap_start + index
    return data[start:data.index(b"\0", start)].decode("utf-8")


def _layout(data, table_stream):
    """(row counts, offset of each table's first row, row sizes, width of a column in bytes) from the #~ stream."""
    start, _ = table_stream
    heap_sizes = _u(data, start + 6, 1, "the table stream header")
    valid = _u(data, start + 8, 8, "the table stream header")
    cursor = start + 24
    counts = {}
    for table in range(64):
        if valid >> table & 1:
            counts[table] = _u(data, cursor, 4, "a row count")
            cursor += 4
    if heap_sizes & 0x40:
        cursor += 4
    wide = {STR: bool(heap_sizes & 1), GUID: bool(heap_sizes & 2), BLOB: bool(heap_sizes & 4)}

    def width(column):
        if isinstance(column, int):
            return column
        if column in wide:
            return 4 if wide[column] else 2
        kind, target = column
        if kind == "table":
            return 4 if counts.get(target, 0) > 0xFFFF else 2
        bits, tables = CODED[target]
        return 4 if max(counts.get(t, 0) for t in tables) >= 1 << (16 - bits) else 2

    offsets, sizes = {}, {}
    for table in sorted(counts):
        if table not in TABLES:
            if table <= ASSEMBLY_REF:
                raise IdentityError(f"metadata table {table:#x} is not known to this reader")
            break  # the tables after AssemblyRef are not needed
        sizes[table] = sum(width(c) for c in TABLES[table])
        offsets[table] = cursor
        cursor += sizes[table] * counts[table]
    return counts, offsets, sizes, width


def read_assembly(data):
    """(name, (a, b, c, d), [(referenced name, (a, b, c, d))]) of one managed assembly image."""
    root, _ = _metadata(data)
    streams = _streams(data, root)
    table_stream = streams.get("#~") or streams.get("#-")
    if table_stream is None or "#Strings" not in streams:
        raise IdentityError("the metadata has no #~ table stream or no #Strings heap")
    counts, offsets, sizes, width = _layout(data, table_stream)
    strings = streams["#Strings"][0]
    if counts.get(ASSEMBLY, 0) != 1:
        raise IdentityError(f"the image has {counts.get(ASSEMBLY, 0)} Assembly rows, not 1: it is a module, not an assembly")

    def row_fields(table, row):
        """The columns of one row as ints (heap indexes and coded values are left as numbers)."""
        out, cursor = [], offsets[table] + sizes[table] * row
        for column in TABLES[table]:
            size = width(column)
            out.append(_u(data, cursor, size, f"row {row} of table {table:#x}"))
            cursor += size
        return out

    own = row_fields(ASSEMBLY, 0)  # HashAlgId, Major, Minor, Build, Revision, Flags, PublicKey, Name, Culture
    name = _string(data, strings, own[7])
    refs = []
    for row in range(counts.get(ASSEMBLY_REF, 0)):
        f = row_fields(ASSEMBLY_REF, row)  # Major, Minor, Build, Revision, Flags, PublicKeyOrToken, Name, Culture, HashValue
        refs.append((_string(data, strings, f[6]), (f[0], f[1], f[2], f[3])))
    return name, (own[1], own[2], own[3], own[4]), refs


def records(framework, data):
    name, version, refs = read_assembly(data)
    text = lambda v: ".".join(str(n) for n in v)
    return [f"{framework}\tDEF\t{name}\t{text(version)}"] + [f"{framework}\tREF\t{name}->{target}\t{text(v)}" for target, v in refs]


def package_identity(path):
    """The sorted identity records of every runtime assembly (.dll under lib/<framework>/) of a package."""
    lines = []
    with zipfile.ZipFile(path) as archive:
        for entry in archive.infolist():
            parts = entry.filename.split("/")
            if parts[0] != "lib" or not entry.filename.lower().endswith(".dll"):
                continue
            if len(parts) <= 2:
                raise IdentityError(f"{entry.filename} is not under a target framework folder")
            try:
                lines += records(parts[1], archive.read(entry))
            except (IdentityError, ValueError, IndexError) as e:
                raise IdentityError(f"{path}: {entry.filename}: {e}")
    return sorted(lines)


def main(argv=None):
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("cmd", choices=["identity"])
    ap.add_argument("--package", required=True)
    ap.add_argument("--out", required=True)
    a = ap.parse_args(argv)
    try:
        lines = package_identity(a.package)
    except (IdentityError, OSError, zipfile.BadZipFile) as e:
        sys.exit(f"assemblyid: {e}")
    with open(a.out, "w", encoding="utf-8", newline="\n") as f:
        f.write("\n".join(lines) + ("\n" if lines else ""))


if __name__ == "__main__":
    main()
