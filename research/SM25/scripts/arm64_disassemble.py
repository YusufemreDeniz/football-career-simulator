#!/usr/bin/env python3
"""Disassemble an AArch64 method from an ELF virtual address."""

from __future__ import annotations

import argparse
import sys
from pathlib import Path

SCRIPT_DIR = Path(__file__).resolve().parent
sys.path.insert(0, str(SCRIPT_DIR))
from elf_utils import Elf64, parse_int


for dependency_directory in (
    SCRIPT_DIR.parent / "tools" / "python_deps_local",
    SCRIPT_DIR.parent / "tools" / "python_deps",
):
    if dependency_directory.is_dir():
        sys.path.insert(0, str(dependency_directory))

try:
    from capstone import CS_ARCH_ARM64, CS_MODE_ARM, Cs
except ImportError as error:  # pragma: no cover - environment diagnostic
    raise SystemExit(
        "Capstone is missing. Install it into research/SM25/tools/python_deps_local "
        "with: python -m pip install --target research/SM25/tools/python_deps_local capstone==5.0.6"
    ) from error


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("elf", type=Path)
    parser.add_argument("virtual_address", type=parse_int)
    parser.add_argument("size", type=parse_int)
    parser.add_argument("--stop-at-ret", action="store_true")
    parser.add_argument("--output", type=Path)
    args = parser.parse_args()

    if args.size <= 0:
        raise ValueError("size must be positive")
    elf = Elf64(args.elf)
    raw, segment = elf.va_to_raw(args.virtual_address)
    if raw + args.size > segment.offset + segment.file_size:
        raise ValueError("Disassembly range crosses the file-backed segment boundary")
    with args.elf.open("rb") as stream:
        stream.seek(raw)
        data = stream.read(args.size)
    if len(data) != args.size:
        raise ValueError("Disassembly range crosses the end of the file")

    engine = Cs(CS_ARCH_ARM64, CS_MODE_ARM)
    lines = [
        f"ELF: {args.elf}",
        f"VA: 0x{args.virtual_address:X}",
        f"RAW: 0x{raw:X}",
        f"SIZE: 0x{args.size:X}",
        "",
    ]
    consumed = 0
    for instruction in engine.disasm(data, args.virtual_address):
        consumed += instruction.size
        lines.append(
            f"0x{instruction.address:08X}  {instruction.bytes.hex(' ').upper():<11}  "
            f"{instruction.mnemonic:<8} {instruction.op_str}".rstrip()
        )
        if args.stop_at_ret and instruction.mnemonic == "ret":
            break
    if not args.stop_at_ret and consumed != len(data):
        lines.append(f"WARNING: disassembler consumed 0x{consumed:X} of 0x{len(data):X} bytes")
    output = "\n".join(lines) + "\n"
    if args.output:
        args.output.parent.mkdir(parents=True, exist_ok=True)
        args.output.write_text(output, encoding="utf-8")
    print(output, end="")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
