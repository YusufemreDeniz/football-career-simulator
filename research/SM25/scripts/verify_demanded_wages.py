#!/usr/bin/env python3
"""SM25 Demanded Wages (+0x24) native ARM64 zincirini doğrulayan sürüm kilitli test script'i."""

from __future__ import annotations

import hashlib
import struct
import sys
from pathlib import Path

SCRIPT_DIR = Path(__file__).resolve().parent
sys.path.insert(0, str(SCRIPT_DIR))
from elf_utils import Elf64

LIBIL2CPP = SCRIPT_DIR.parent / "il2cpp" / "libil2cpp.so"
EXPECTED_LIB_SHA256 = "d9ae3187294421faf00656e1df117744ad5ceffa14edd801df8af785e322ab73"


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def read_bytes(elf: Elf64, virtual_address: int, size: int) -> bytes:
    raw, segment = elf.va_to_raw(virtual_address)
    if raw + size > segment.offset + segment.file_size:
        raise AssertionError(f"0x{virtual_address:X} okuması ELF bölüm sınırını aşıyor")
    with LIBIL2CPP.open("rb") as stream:
        stream.seek(raw)
        data = stream.read(size)
    if len(data) != size:
        raise AssertionError(f"0x{virtual_address:X} için eksik veri okundu")
    return data


def assert_instruction(elf: Elf64, address: int, expected_hex: str, description: str) -> None:
    expected = bytes.fromhex(expected_hex)
    actual = read_bytes(elf, address, len(expected))
    assert actual == expected, (
        f"{description}: 0x{address:X} adresinde {actual.hex(' ').upper()} okundu, "
        f"{expected.hex(' ').upper()} bekleniyordu"
    )


def decode_call_target(elf: Elf64, address: int) -> int:
    word = struct.unpack("<I", read_bytes(elf, address, 4))[0]
    assert word & 0xFC000000 == 0x94000000, (
        f"0x{address:X} doğrudan AArch64 BL talimatı değil: 0x{word:08X}"
    )
    immediate = word & 0x03FFFFFF
    if immediate & 0x02000000:
        immediate -= 0x04000000
    return address + (immediate << 2)


def assert_call(elf: Elf64, address: int, target: int, description: str) -> None:
    actual = decode_call_target(elf, address)
    assert actual == target, (
        f"{description}: 0x{address:X} çağrısı 0x{actual:X}, beklenen 0x{target:X}"
    )


def assert_float64(elf: Elf64, address: int, expected: float, description: str) -> None:
    actual = struct.unpack("<d", read_bytes(elf, address, 8))[0]
    assert actual == expected, f"{description}: {actual}, beklenen {expected}"


def verify_demanded_wages() -> None:
    assert sha256(LIBIL2CPP) == EXPECTED_LIB_SHA256, (
        "libil2cpp.so SHA256 değişti; verifier FAIL verdi"
    )
    elf = Elf64(LIBIL2CPP)
    assert elf.machine == 183, f"Beklenmeyen ELF makine türü: {elf.machine}"

    # 1. ContractInfo.DemandedWages (+0x24) yazma noktaları
    assert_instruction(elf, 0x030278F4, "01 24 00 B9", "set_DemandedWages: str w1, [x0, #0x24]")
    assert_instruction(elf, 0x03027B74, "80 26 00 B9", "RecalculateContract: str w0, [x20, #0x24]")
    assert_instruction(elf, 0x03027DB0, "69 26 00 B9", "GetNegotiationWageForUserManager: str w9, [x19, #0x24]")
    assert_instruction(elf, 0x0303A964, "28 25 00 B9", "RejectContractOffer: str w8, [x9, #0x24]")
    assert_instruction(elf, 0x0303B2D4, "80 26 00 B9", "TryAcceptContractOffer gate: str w0, [x20, #0x24]")

    # 2. PlayerContract.LengthMultiplier: 1.0 + (length - 1) / 40.0
    assert_instruction(elf, 0x02FBFCB4, "28 04 00 51", "LengthMultiplier: sub w8, w1, #1")
    assert_instruction(elf, 0x02FBFCBC, "88 08 E8 D2", "LengthMultiplier: mov x8, #0x4044000000000000 (40.0)")
    assert_instruction(elf, 0x02FBFCCC, "00 28 61 1E", "LengthMultiplier: fadd d0, d0, d1 (1.0 + x)")

    # 3. PlayerContract.GenerateNewContractWages: Traits & £50 clamp
    assert_instruction(elf, 0x02FBFDFC, "C1 04 80 52", "Transfer trait check: mov w1, #0x26 (Trait 38 HardBargain)")
    assert_instruction(elf, 0x02FBFEC4, "81 04 80 52", "Renewal trait check: mov w1, #0x24 (Trait 36 ToughNegotiator)")
    assert_instruction(elf, 0x02FBFF48, "41 06 80 52", "Minimum wage clamp: mov w1, #0x32 (50)")
    assert_call(elf, 0x02FBFF50, 0x058D9770, "Math.Max(wage, 50) call")
    assert_instruction(elf, 0x02FBFF54, "60 1A 00 B9", "PlayerContract.Wage write: str w0, [x19, #0x18]")

    # 4. PlayerData.GetAcceptableNewContractWage: Unwilling renewal +50% penalty
    assert_instruction(elf, 0x02FC3F5C, "88 A6 94 1A", "Unwilling penalty cinc: cinc w8, w20, lt")
    assert_instruction(elf, 0x02FC3F60, "94 06 88 0B", "Unwilling penalty add: add w20, w20, w8, asr #1 (wage * 1.5)")

    # 5. PlayerData.GetRequestedWageMultiplier: Rejection RNG [1.0, 1.1] & Seed
    assert_instruction(elf, 0x02FC3A90, "88 12 40 B9", "PlayerId read: ldr w8, [x20, #0x10]")
    assert_instruction(elf, 0x02FC3A94, "89 02 C1 79", "ClubId read: ldrsh w9, [x20, #0x80]")
    assert_instruction(elf, 0x02FC3A9C, "01 01 09 0B", "Rejection seed: add w1, w8, w9 (PlayerId + ClubId)")
    assert_instruction(elf, 0x02FC3AC0, "00 10 6E 1E", "Rejection min multiplier: fmov d0, #1.00000000")
    assert_float64(elf, 0x01332960, 1.1, "Rejection max multiplier: 1.1")
    assert_call(elf, 0x02FC3FA8, 0x031ECC38, "MonetaryHelpers.RoundMonetaryValue call")

    # 6. ContractInfo.GetNegotiationWageForUserManager: UI Displayed Demand [1.10, 1.25]
    assert_instruction(elf, 0x03027DD4, "21 01 08 0B", "UI modal seed: add w1, w9, w8 (PlayerId + Game.Seed)")
    assert_instruction(elf, 0x03027DF8, "02 90 6E 1E", "UI modal max multiplier: fmov d2, #1.25000000")
    assert_instruction(elf, 0x03027E1C, "80 0C 80 52", "UI modal round to 100: mov w0, #0x64")

    print("VERIFY_DEMANDED_WAGES=PASS")
    print("ENTRY_POINTS=0x030278F4,0x03027B74,0x03027DB0,0x0303A964,0x0303B2D4")
    print("LENGTH_MULTIPLIER=1.0+(length-1)/40.0 (+2.5%/year)")
    print("TRAITS=38_HardBargain(Transfer),36_ToughNegotiator(Renewal)")
    print("UNWILLING_RENEWAL_PENALTY=+50% (wage+wage/2)")
    print("MINIMUM_WAGE=£50 (Math.Max(wage, 50))")
    print("REJECTION_RNG=1.00..1.10 (Seed: PlayerId + ClubId, MonetaryHelpers.Round)")
    print("DISPLAYED_RNG=1.10..1.25 (Seed: PlayerId + Game.Seed, Round to 100)")


def main() -> int:
    if not LIBIL2CPP.is_file():
        raise SystemExit("libil2cpp.so dosyası bulunamadı")
    verify_demanded_wages()
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
