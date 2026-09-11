#!/usr/bin/env python3
"""Small dependency-free ELF64 helper used by the SM25 research scripts."""

from __future__ import annotations

import argparse
import dataclasses
import json
import struct
from pathlib import Path


PT_LOAD = 1


@dataclasses.dataclass(frozen=True)
class ProgramHeader:
    index: int
    type: int
    flags: int
    offset: int
    virtual_address: int
    file_size: int
    memory_size: int
    alignment: int

    @property
    def permissions(self) -> str:
        return "".join(("R" if self.flags & 4 else "-", "W" if self.flags & 2 else "-", "X" if self.flags & 1 else "-"))

    def contains_va(self, address: int) -> bool:
        return self.virtual_address <= address < self.virtual_address + self.file_size

    def contains_raw(self, offset: int) -> bool:
        return self.offset <= offset < self.offset + self.file_size


class Elf64:
    def __init__(self, path: Path | str):
        self.path = Path(path)
        with self.path.open("rb") as stream:
            header = stream.read(64)
            if len(header) != 64 or header[:4] != b"\x7fELF":
                raise ValueError(f"Not an ELF file: {self.path}")
            if header[4] != 2:
                raise ValueError("Only ELF64 is supported")
            if header[5] != 1:
                raise ValueError("Only little-endian ELF is supported")
            self.machine = struct.unpack_from("<H", header, 18)[0]
            self.program_header_offset = struct.unpack_from("<Q", header, 32)[0]
            self.program_header_entry_size = struct.unpack_from("<H", header, 54)[0]
            self.program_header_count = struct.unpack_from("<H", header, 56)[0]
            if self.program_header_entry_size < 56:
                raise ValueError("Unexpected ELF64 program-header size")

            headers: list[ProgramHeader] = []
            for index in range(self.program_header_count):
                stream.seek(self.program_header_offset + index * self.program_header_entry_size)
                data = stream.read(self.program_header_entry_size)
                if len(data) != self.program_header_entry_size:
                    raise ValueError("Truncated ELF program-header table")
                values = struct.unpack_from("<IIQQQQQQ", data)
                headers.append(
                    ProgramHeader(
                        index=index,
                        type=values[0],
                        flags=values[1],
                        offset=values[2],
                        virtual_address=values[3],
                        file_size=values[5],
                        memory_size=values[6],
                        alignment=values[7],
                    )
                )
            self.program_headers = tuple(headers)

    @property
    def load_segments(self) -> tuple[ProgramHeader, ...]:
        return tuple(header for header in self.program_headers if header.type == PT_LOAD)

    def segment_for_va(self, address: int) -> ProgramHeader:
        for segment in self.load_segments:
            if segment.contains_va(address):
                return segment
        raise ValueError(f"VA 0x{address:X} is not backed by a PT_LOAD file range")

    def segment_for_raw(self, offset: int) -> ProgramHeader:
        for segment in self.load_segments:
            if segment.contains_raw(offset):
                return segment
        raise ValueError(f"RAW 0x{offset:X} is not inside a PT_LOAD file range")

    def va_to_raw(self, address: int) -> tuple[int, ProgramHeader]:
        segment = self.segment_for_va(address)
        return segment.offset + address - segment.virtual_address, segment

    def raw_to_va(self, offset: int) -> tuple[int, ProgramHeader]:
        segment = self.segment_for_raw(offset)
        return segment.virtual_address + offset - segment.offset, segment


def parse_int(value: str) -> int:
    return int(value, 0)


def _segment_dict(segment: ProgramHeader) -> dict[str, int | str]:
    return {
        "index": segment.index,
        "flags": segment.flags,
        "permissions": segment.permissions,
        "offset": segment.offset,
        "virtual_address": segment.virtual_address,
        "file_size": segment.file_size,
        "memory_size": segment.memory_size,
        "alignment": segment.alignment,
    }


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("elf", type=Path)
    parser.add_argument("--json", action="store_true")
    subparsers = parser.add_subparsers(dest="command", required=True)
    subparsers.add_parser("segments")
    va_parser = subparsers.add_parser("va-to-raw")
    va_parser.add_argument("address", type=parse_int)
    raw_parser = subparsers.add_parser("raw-to-va")
    raw_parser.add_argument("offset", type=parse_int)
    args = parser.parse_args()

    elf = Elf64(args.elf)
    if args.command == "segments":
        result = [_segment_dict(segment) for segment in elf.load_segments]
    elif args.command == "va-to-raw":
        raw, segment = elf.va_to_raw(args.address)
        result = {"virtual_address": args.address, "raw_offset": raw, "segment": _segment_dict(segment)}
    else:
        virtual_address, segment = elf.raw_to_va(args.offset)
        result = {"raw_offset": args.offset, "virtual_address": virtual_address, "segment": _segment_dict(segment)}

    if args.json:
        print(json.dumps(result, indent=2))
    elif isinstance(result, list):
        print("idx permissions virtual_address raw_offset file_size memory_size")
        for item in result:
            print(
                f"{item['index']:>3} {item['permissions']:>11} "
                f"0x{item['virtual_address']:08X} 0x{item['offset']:08X} "
                f"0x{item['file_size']:X} 0x{item['memory_size']:X}"
            )
    else:
        print(f"VA=0x{result['virtual_address']:X}")
        print(f"RAW=0x{result['raw_offset']:X}")
        segment = result["segment"]
        print(
            f"SEGMENT[{segment['index']}] VA=0x{segment['virtual_address']:X} "
            f"RAW=0x{segment['offset']:X} FLAGS=0x{segment['flags']:X} ({segment['permissions']})"
        )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
