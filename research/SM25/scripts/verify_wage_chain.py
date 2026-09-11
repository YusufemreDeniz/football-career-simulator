#!/usr/bin/env python3
"""Independent native verifier for the SM25 weekly wage calculation chain."""

from __future__ import annotations

import struct
import sys
from pathlib import Path

SCRIPT_DIR = Path(__file__).resolve().parent
sys.path.insert(0, str(SCRIPT_DIR))
from elf_utils import Elf64

LIBIL2CPP = SCRIPT_DIR.parent / "il2cpp" / "libil2cpp.so"


def verify_wage_chain() -> int:
    if not LIBIL2CPP.is_file():
        raise SystemExit(f"Missing {LIBIL2CPP}")

    elf = Elf64(LIBIL2CPP)

    # 1. Verify PlayerContract.GenerateNewContractWages(double, double)
    # VA: 0x02FC0160, RAW: 0x02FBC160
    # Expected: fmul d0, d9, d8; cmp/truncate; Math.Max(w0, 50); store to Wage (offset 0x18)
    gen_wages_va = 0x02FC0160
    gen_wages_raw, _ = elf.va_to_raw(gen_wages_va)
    with LIBIL2CPP.open("rb") as f:
        f.seek(gen_wages_raw)
        gen_wages_bytes = f.read(0x90)

    # Check for 'mov w1, #0x32' (0x32 = 50 minimum wage floor): 41 06 80 52
    assert bytes.fromhex("41 06 80 52") in gen_wages_bytes, "Missing wage minimum floor (50) in GenerateNewContractWages"

    # 2. Verify PlayerContract.GenerateInitialWage
    # VA: 0x02FC0388, RAW: 0x02FBC388
    # Expected: sub w8, w20, #1; div by 40.0; add 1.0; call GenerateNewContractWages; call RoundMonetaryValue
    gen_init_va = 0x02FC0388
    gen_init_raw, _ = elf.va_to_raw(gen_init_va)
    with LIBIL2CPP.open("rb") as f:
        f.seek(gen_init_raw)
        gen_init_bytes = f.read(0x60)

    # Check for 'mov x8, #0x4044000000000000' (40.0 length divisor): 88 08 E8 D2
    assert bytes.fromhex("88 08 E8 D2") in gen_init_bytes, "Missing 40.0 length scale in GenerateInitialWage"

    # 3. Verify PlayerContract.Generate
    # VA: 0x02FBFB30, RAW: 0x02FBBB30
    gen_va = 0x02FBFB30
    gen_raw, _ = elf.va_to_raw(gen_va)
    with LIBIL2CPP.open("rb") as f:
        f.seek(gen_raw)
        gen_bytes = f.read(0x100)

    assert bytes.fromhex("88 08 E8 D2") in gen_bytes, "Missing 40.0 length scale in Generate"

    # 4. Verify MonetaryHelpers.RoundMonetaryValue constants
    # VA: 0x031ECC38, RAW: 0x031E8C38
    round_va = 0x031ECC38
    round_raw, _ = elf.va_to_raw(round_va)

    constant_checks = [
        (0x013332F0, 250.0, "Threshold 250.0"),
        (0x013337D0, 1000.0, "Threshold 1000.0"),
        (0x013338F8, 5000.0, "Threshold 5000.0"),
        (0x01332830, 10000.0, "Threshold 10000.0"),
        (0x01332250, 50000.0, "Threshold 50000.0"),
        (0x01332D70, 500.0, "Step 500.0"),
        (0x01332B40, 100000.0, "Threshold 100000.0"),
        (0x01333C88, 5000000.0, "Threshold 5000000.0"),
        (0x01333530, 1000000.0, "Threshold 1000000.0 (shadowed)"),
        (0x01333C68, 10000000.0, "Threshold 10000000.0"),
        (0x01333200, 50000000.0, "Threshold 50000000.0"),
        (0x01334350, 1000000.0, "Step 1000000.0 (>= 50M)"),
        (0x01334358, 500000.0, "Step 500000.0 (< 50M)"),
    ]

    with LIBIL2CPP.open("rb") as f:
        for va, expected, desc in constant_checks:
            raw, _ = elf.va_to_raw(va)
            f.seek(raw)
            val = struct.unpack("<d", f.read(8))[0]
            assert val == expected, f"Mismatch for {desc}: got {val}, expected {expected}"

    print("VERIFY_WAGE_CHAIN=PASS")
    print(f"GENERATE_NEW_CONTRACT_WAGES_VA=0x{gen_wages_va:X}")
    print(f"GENERATE_INITIAL_WAGE_VA=0x{gen_init_va:X}")
    print(f"ROUND_MONETARY_VALUE_VA=0x{round_va:X}")
    print("LENGTH_MULTIPLIER_FORMULA=1.0 + (contractLength - 1) / 40.0")
    print("WAGE_MINIMUM_FLOOR=50")
    return 0


if __name__ == "__main__":
    raise SystemExit(verify_wage_chain())
