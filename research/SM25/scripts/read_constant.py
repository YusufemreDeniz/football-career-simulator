#!/usr/bin/env python3
"""Read a typed little-endian value from an ELF virtual address or raw offset."""

from __future__ import annotations

import argparse
import json
import struct
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from elf_utils import Elf64, parse_int


FORMATS = {
    "byte": "B",
    "int16": "h",
    "uint16": "H",
    "int32": "i",
    "uint32": "I",
    "int64": "q",
    "uint64": "Q",
    "float32": "f",
    "float64": "d",
}


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("elf", type=Path)
    parser.add_argument("address", type=parse_int)
    parser.add_argument("type", choices=FORMATS)
    parser.add_argument("--mode", choices=("va", "raw"), required=True)
    parser.add_argument("--json", action="store_true")
    args = parser.parse_args()

    elf = Elf64(args.elf)
    if args.mode == "va":
        raw, segment = elf.va_to_raw(args.address)
        virtual_address = args.address
    else:
        raw = args.address
        virtual_address, segment = elf.raw_to_va(raw)

    size = struct.calcsize("<" + FORMATS[args.type])
    if raw + size > segment.offset + segment.file_size:
        raise ValueError("Requested value crosses the file-backed segment boundary")
    with args.elf.open("rb") as stream:
        stream.seek(raw)
        data = stream.read(size)
    if len(data) != size:
        raise ValueError("Requested value crosses the end of the file")
    value = struct.unpack("<" + FORMATS[args.type], data)[0]
    result = {
        "mode": args.mode,
        "input_address": args.address,
        "virtual_address": virtual_address,
        "raw_offset": raw,
        "type": args.type,
        "bytes": data.hex(" ").upper(),
        "value": value,
    }
    if args.json:
        print(json.dumps(result, indent=2, allow_nan=False))
    else:
        print(f"VA=0x{virtual_address:X}")
        print(f"RAW=0x{raw:X}")
        print(f"TYPE={args.type}")
        print(f"BYTES={result['bytes']}")
        print(f"VALUE={value!r}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
