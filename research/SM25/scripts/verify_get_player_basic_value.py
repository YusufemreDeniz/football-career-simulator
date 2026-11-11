#!/usr/bin/env python3
"""Re-run the native evidence checks for PlayerData.GetPlayerBasicValue."""

from __future__ import annotations

import struct
import sys
from pathlib import Path


SCRIPT_DIR = Path(__file__).resolve().parent
sys.path.insert(0, str(SCRIPT_DIR))
for dependency_directory in (
    SCRIPT_DIR.parent / "tools" / "python_deps_local",
    SCRIPT_DIR.parent / "tools" / "python_deps",
):
    if dependency_directory.is_dir():
        sys.path.insert(0, str(dependency_directory))

from capstone import CS_ARCH_ARM64, CS_MODE_ARM, Cs

from elf_utils import Elf64


METHOD_VA = 0x02FC3FBC
METHOD_SIZE = 0x14C
EXPECTED_RAW = 0x02FBFFBC
CONSTANTS = {
    0x013325B8: 99.0,
    0x01332700: 750000.0,
    0x01332950: 0.1,
    0x01333088: 0.9,
}


def main() -> int:
    elf_path = SCRIPT_DIR.parent / "il2cpp" / "libil2cpp.so"
    elf = Elf64(elf_path)
    raw, segment = elf.va_to_raw(METHOD_VA)
    assert raw == EXPECTED_RAW, (hex(raw), hex(EXPECTED_RAW))
    assert segment.flags == 0x5, hex(segment.flags)

    with elf_path.open("rb") as stream:
        stream.seek(raw)
        method_bytes = stream.read(METHOD_SIZE)
    instructions = {instruction.address: instruction for instruction in Cs(CS_ARCH_ARM64, CS_MODE_ARM).disasm(method_bytes, METHOD_VA)}
    expected = {
        0x02FC3FD8: ("mov", "w19, w3"),
        0x02FC3FE4: ("mov", "w21, w2"),
        0x02FC3FE8: ("mov", "w20, w1"),
        0x02FC400C: ("tbz", "w21, #0, #0x2fc4058"),
        0x02FC401C: ("fmov", "d1, #20.00000000"),
        0x02FC4024: ("ucvtf", "d0, w8"),
        0x02FC4044: ("fdiv", "d0, d8, d0"),
        0x02FC4090: ("scvtf", "d2, w19"),
        0x02FC40BC: ("fdiv", "d1, d8, d10"),
        0x02FC40D4: ("fdiv", "d0, d11, d0"),
        0x02FC40E4: ("fmul", "d1, d1, d2"),
        0x02FC40E8: ("fmul", "d0, d0, d3"),
        0x02FC40EC: ("fadd", "d0, d1, d0"),
        0x02FC4104: ("ret", ""),
    }
    for address, (mnemonic, operands) in expected.items():
        instruction = instructions[address]
        assert instruction.mnemonic == mnemonic, (hex(address), instruction.mnemonic, mnemonic)
        assert instruction.op_str == operands, (hex(address), instruction.op_str, operands)

    with elf_path.open("rb") as stream:
        for address, expected_value in CONSTANTS.items():
            constant_raw, _ = elf.va_to_raw(address)
            stream.seek(constant_raw)
            value = struct.unpack("<d", stream.read(8))[0]
            assert value == expected_value, (hex(address), value, expected_value)

    print("VERIFY_GET_PLAYER_BASIC_VALUE=PASS")
    print(f"METHOD_VA=0x{METHOD_VA:X}")
    print(f"METHOD_RAW=0x{raw:X}")
    print("FORMULA_PEAKED=750000*(rating/99)^20")
    print("FORMULA_NOT_PEAKED=750000*(0.9*(rating/99)^20+0.1*(potential/99)^20)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
