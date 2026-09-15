#!/usr/bin/env python3
"""SM25 haftalık ücret zincirinin adres, talimat, çağrı ve sabit doğrulayıcısı."""

from __future__ import annotations

import struct
import sys
from pathlib import Path

SCRIPT_DIR = Path(__file__).resolve().parent
sys.path.insert(0, str(SCRIPT_DIR))
from elf_utils import Elf64

LIBIL2CPP = SCRIPT_DIR.parent / "il2cpp" / "libil2cpp.so"


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


def assert_instruction(elf: Elf64, virtual_address: int, expected_hex: str, description: str) -> None:
    expected = bytes.fromhex(expected_hex)
    actual = read_bytes(elf, virtual_address, len(expected))
    assert actual == expected, (
        f"{description}: 0x{virtual_address:X} adresinde {actual.hex(' ').upper()} okundu, "
        f"{expected.hex(' ').upper()} bekleniyordu"
    )


def decode_direct_branch_target(elf: Elf64, virtual_address: int, expected_opcode: int) -> int:
    word = struct.unpack("<I", read_bytes(elf, virtual_address, 4))[0]
    assert word & 0xFC000000 == expected_opcode, (
        f"0x{virtual_address:X} doğrudan AArch64 dal/çağrı talimatı değil: 0x{word:08X}"
    )
    immediate = word & 0x03FFFFFF
    if immediate & 0x02000000:
        immediate -= 0x04000000
    return virtual_address + (immediate << 2)


def assert_call(elf: Elf64, virtual_address: int, target: int, description: str) -> None:
    actual = decode_direct_branch_target(elf, virtual_address, 0x94000000)
    assert actual == target, (
        f"{description}: 0x{virtual_address:X} çağrısı 0x{actual:X} hedefine gidiyor, "
        f"0x{target:X} bekleniyordu"
    )


def assert_branch(elf: Elf64, virtual_address: int, target: int, description: str) -> None:
    actual = decode_direct_branch_target(elf, virtual_address, 0x14000000)
    assert actual == target, (
        f"{description}: 0x{virtual_address:X} dalı 0x{actual:X} hedefine gidiyor, "
        f"0x{target:X} bekleniyordu"
    )


def assert_float64(elf: Elf64, virtual_address: int, expected: float, description: str) -> None:
    actual = struct.unpack("<d", read_bytes(elf, virtual_address, 8))[0]
    assert actual == expected, f"{description}: {actual} okundu, {expected} bekleniyordu"


def verify_wage_chain() -> int:
    if not LIBIL2CPP.is_file():
        raise SystemExit(f"Eksik ikili dosya: {LIBIL2CPP}")

    elf = Elf64(LIBIL2CPP)
    assert elf.machine == 183, f"Beklenmeyen ELF makine türü: {elf.machine}"

    # Saklanan mevcut haftalık ücret: PlayerContract.Wage alanı +0x18.
    assert_instruction(elf, 0x02FBFB18, "00 18 40 B9", "PlayerContract.get_Wage alan okuması")
    assert_instruction(elf, 0x02FBFB1C, "C0 03 5F D6", "PlayerContract.get_Wage dönüşü")
    assert_instruction(elf, 0x02FC01F8, "28 18 40 B9", "CreateNew teklif ücretini okur")
    assert_instruction(elf, 0x02FC01FC, "08 18 00 B9", "CreateNew kabul edilen ücreti saklar")

    # Uzunluk: 1 + (yıl - 1) / 40.
    assert_instruction(elf, 0x02FBFCB4, "28 04 00 51", "Sözleşme yılından bir çıkarma")
    assert_instruction(elf, 0x02FBFCBC, "88 08 E8 D2", "40,0 uzunluk böleni")
    assert_instruction(elf, 0x02FBFCC4, "00 18 61 1E", "Uzunluk bölme işlemi")
    assert_instruction(elf, 0x02FBFCCC, "00 28 61 1E", "Uzunluk çarpanına 1,0 ekleme")

    # Basit overload: çarp, sıfıra doğru kes, 50 tabanı uygula, Wage alanına yaz.
    assert_instruction(elf, 0x02FC01B0, "20 09 68 1E", "Ücret ve uzunluk çarpımı")
    assert_instruction(elf, 0x02FC01B8, "41 06 80 52", "50 haftalık ücret tabanı")
    assert_instruction(elf, 0x02FC01C8, "09 00 78 1E", "Tamsayıya sıfıra doğru kesme")
    assert_call(elf, 0x02FC01D4, 0x058D9770, "Math.Max ücret tabanı çağrısı")
    assert_instruction(elf, 0x02FC01D8, "60 1A 00 B9", "Wage +0x18 alanına yazma")

    # Kompleks overload: Club yönü, yalnızca ilgili menajer özelliği ve son 50 tabanı.
    assert_call(elf, 0x02FBFD40, 0x02FC08B4, "WageModifierForClub(Club) çağrısı")
    assert_instruction(elf, 0x02FBFD44, "00 08 68 1E", "Kulüp ücretini uzunlukla çarpma")
    assert_instruction(elf, 0x02FBFDFC, "C1 04 80 52", "Transfer görüşmesi özellik kimliği 38")
    assert_call(elf, 0x02FBFE04, 0x030544E4, "Transfer özelliğini okuma")
    assert_instruction(elf, 0x02FBFE24, "01 24 40 BD", "Özellik yüzdesini +0x24 alanından okuma")
    assert_call(elf, 0x02FBFE2C, 0x02FD6058, "Transfer yüzdesini uygulama")
    assert_instruction(elf, 0x02FBFEC4, "81 04 80 52", "Yenileme görüşmesi özellik kimliği 36")
    assert_call(elf, 0x02FBFECC, 0x030544E4, "Yenileme özelliğini okuma")
    assert_call(elf, 0x02FBFEF4, 0x02FD6058, "Yenileme yüzdesini uygulama")
    assert_instruction(elf, 0x02FBFF48, "41 06 80 52", "Kompleks zincirde 50 tabanı")
    assert_call(elf, 0x02FBFF50, 0x058D9770, "Kompleks zincirde Math.Max")
    assert_instruction(elf, 0x02FBFF54, "60 1A 00 B9", "Kompleks zincirde Wage alanına yazma")

    # Yüzde yardımcısı: original * (1 + percentage / 100).
    assert_instruction(elf, 0x02FD6058, "08 59 A8 52", "Yüzde yardımcısında 100,0 sabiti")
    assert_instruction(elf, 0x02FD6060, "21 18 22 1E", "Yüzdeyi 100'e bölme")
    assert_instruction(elf, 0x02FD6068, "21 28 22 1E", "Yüzde oranına 1,0 ekleme")
    assert_instruction(elf, 0x02FD606C, "20 08 20 1E", "Asıl değeri yüzde çarpanıyla çarpma")

    # Otomatik sözleşme üretimi: kompleks ücret, personel ID 9 ve para yuvarlama.
    assert_call(elf, 0x02FBFBC4, 0x02FBFCD4, "Generate kompleks ücret çağrısı")
    assert_instruction(elf, 0x02FBFBF8, "20 01 80 52", "WageNegotiator değiştirici kimliği 9")
    assert_call(elf, 0x02FBFC00, 0x02F20E54, "Personel ücret değiştiricisi çağrısı")
    assert_instruction(elf, 0x02F20EDC, "00 18 40 BD", "Personel çarpanını +0x18 alanından okuma")
    assert_instruction(elf, 0x02F20EEC, "00 08 68 1E", "Personel çarpanını ücrete uygulama")
    assert_call(elf, 0x02FBFC28, 0x031ECC38, "Nihai parasal yuvarlama çağrısı")

    # Talep: kabul edilebilir ücret * seeded deterministic random [1,00, 1,10], sonra yuvarlama.
    assert_instruction(elf, 0x02FC3AC0, "00 10 6E 1E", "Talep çarpanı alt sınırı 1,00")
    assert_float64(elf, 0x01332960, 1.1, "Talep çarpanı üst sınırı 1,10")
    assert_call(elf, 0x02FC3ACC, 0x031F3F6C, "Tohumlu deterministik rastgele talep RNG çağrısı")
    assert_call(elf, 0x02FC3F8C, 0x02FC3E9C, "Kabul edilebilir yeni sözleşme ücreti çağrısı")
    assert_call(elf, 0x02FC3F98, 0x02FC3A0C, "Talep çarpanı çağrısı")
    assert_instruction(elf, 0x02FC3FA4, "00 08 61 1E", "Kabul edilebilir ücret ve talep çarpımı")
    assert_call(elf, 0x02FC3FA8, 0x031ECC38, "Talebin parasal yuvarlaması")

    # Kullanıcı arayüzü açılış beklentisi: taban * [1,10, 1,25], 100 adımına yuvarlama.
    assert_call(elf, 0x03027D8C, 0x02FBFCB4, "Görüşme ekranı uzunluk çarpanı")
    assert_call(elf, 0x03027DA0, 0x02FBFCD4, "Görüşme ekranı kompleks ücret çağrısı")
    assert_instruction(elf, 0x03027DB0, "69 26 00 B9", "DemandedWages +0x24 alanına taban yazma")
    assert_instruction(elf, 0x03027DF8, "02 90 6E 1E", "Görüşme beklentisi üst sınırı 1,25")
    assert_float64(elf, 0x01332960, 1.1, "Görüşme beklentisi alt sınırı 1,10")
    assert_call(elf, 0x03027E14, 0x031F3F6C, "Görüşme beklentisi RNG çağrısı")
    assert_instruction(elf, 0x03027E1C, "80 0C 80 52", "Görüşme ekranı yuvarlama adımı 100")
    assert_branch(elf, 0x03027E2C, 0x031DC4B4, "Extensions.Round çağrısına kuyruk dalı")

    # Teklif kabulü: Wage teklif alanı ile DemandedWages eşiğinin ayrılığı.
    assert_instruction(elf, 0x03027CA4, "08 18 40 B9", "WillAcceptWage teklif ücretini okur")
    assert_instruction(elf, 0x03027CA8, "09 24 40 B9", "WillAcceptWage talep eşiğini okur")
    assert_instruction(elf, 0x03027CDC, "48 01 80 52", "WillAcceptWage kabul sonucu 10")

    # Parasal yuvarlama eşikleri ve adımları.
    constant_checks = [
        (0x013332F0, 250.0, "250 eşiği"),
        (0x013337D0, 1_000.0, "1.000 eşiği"),
        (0x013338F8, 5_000.0, "5.000 eşiği"),
        (0x01332830, 10_000.0, "10.000 eşiği"),
        (0x01332250, 50_000.0, "50.000 eşiği"),
        (0x01332B40, 100_000.0, "100.000 eşiği"),
        (0x01333C88, 5_000_000.0, "5.000.000 eşiği"),
        (0x01333530, 1_000_000.0, "gölgelenmiş 1.000.000 karşılaştırması"),
        (0x01333C68, 10_000_000.0, "10.000.000 eşiği"),
        (0x01333200, 50_000_000.0, "50.000.000 eşiği"),
        (0x01334358, 500_000.0, "500.000 adımı"),
        (0x01334350, 1_000_000.0, "1.000.000 adımı"),
    ]
    for virtual_address, expected, description in constant_checks:
        assert_float64(elf, virtual_address, expected, description)

    print("VERIFY_WAGE_CHAIN=PASS")
    print("DOĞRULANAN=mevcut_ücret,uzunluk,kulüp_ve_lig,özellikler,personel,talep,teklif,kabul,yuvarlama")
    print("ÜCRET_ALANI_OFSETİ=0x18")
    print("SÖZLEŞME_UZUNLUĞU_FORMÜLÜ=1.0+(yıl-1)/40.0")
    print("YENİ_SÖZLEŞME_TALEP_ÇARPANI=seeded_deterministic_random_[1.00,1.10]")
    print("GÖRÜŞME_EKRANI_BEKLENTİ_ÇARPANI=seeded_deterministic_random_[1.10,1.25]")
    print("ASGARİ_HAFTALIK_ÜCRET=50")
    return 0


if __name__ == "__main__":
    raise SystemExit(verify_wage_chain())
