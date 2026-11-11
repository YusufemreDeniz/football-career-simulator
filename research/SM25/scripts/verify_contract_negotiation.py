#!/usr/bin/env python3
"""SM25 oyuncu sözleşmesi görüşme zincirinin sürüm kilitli native doğrulayıcısı."""

from __future__ import annotations

import hashlib
import struct
import sys
import zipfile
from pathlib import Path

SCRIPT_DIR = Path(__file__).resolve().parent
sys.path.insert(0, str(SCRIPT_DIR))
from elf_utils import Elf64

LIBIL2CPP = SCRIPT_DIR.parent / "il2cpp" / "libil2cpp.so"
ASSET_PACK = SCRIPT_DIR.parent / "original" / "split_UnityDataAssetPack.apk"
EXPECTED_LIB_SHA256 = "d9ae3187294421faf00656e1df117744ad5ceffa14edd801df8af785e322ab73"
EXPECTED_ASSET_PACK_SHA256 = "793509ef1e28f555c30c7dbb743f3c2420fe6a0d3f5f51f3df1554557160b33d"

ASSET_HASHES = {
    "assets/bin/Data/04294b045640b8049a58735ff0102ad9":
        "f552b53b53a7045ea34b715083020ef1acde1ec11c9e751dfd635e137678829c",
    "assets/bin/Data/1cdfc457ed0f567478cc1c3a7dc46c54":
        "182c2563003f94962bcacd54b9ec3ed98b3e9dff67d2e610677fba284b8e2129",
    "assets/bin/Data/7b3db47fa1abe4a4fb28030a16c32de9":
        "48e15c3246009d681fa573de59591f07800e90fcee616dfb97b29f8c6fcd83f7",
    "assets/bin/Data/a16bfa70e33ea8c429d6455be61b9cfb":
        "285f2dc7a01e1367d61099f5abc09f1b8a07b6efc59fc43b851149cdc091a662",
    "assets/bin/Data/a6fa163d1b729064184ef557944356b5":
        "ca17b3f5702f4caf4454064dca8db90f612d69e3d76d793ba76bd6ec65647d66",
    "assets/bin/Data/d062a764541938d4ea0589ed8f071bd8":
        "cf489deab22284f24ff52bdd47d8ba5f15e4ea78141eba296d7508a65854b215",
}


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


def assert_float32(elf: Elf64, address: int, expected: float, description: str) -> None:
    actual = struct.unpack("<f", read_bytes(elf, address, 4))[0]
    assert abs(actual - expected) < 1e-7, f"{description}: {actual}, beklenen {expected}"


def assert_float64(elf: Elf64, address: int, expected: float, description: str) -> None:
    actual = struct.unpack("<d", read_bytes(elf, address, 8))[0]
    assert actual == expected, f"{description}: {actual}, beklenen {expected}"


def verify_assets() -> None:
    assert sha256(ASSET_PACK) == EXPECTED_ASSET_PACK_SHA256, (
        "Unity veri paketi sürümü değişti; sessizce devam edilmiyor"
    )
    with zipfile.ZipFile(ASSET_PACK) as archive:
        data = {}
        for name, expected_hash in ASSET_HASHES.items():
            payload = archive.read(name)
            actual_hash = hashlib.sha256(payload).hexdigest()
            assert actual_hash == expected_hash, f"Asset değişti: {name}"
            data[name] = payload

    modifiers = data["assets/bin/Data/04294b045640b8049a58735ff0102ad9"]
    assert b'"Name": "attr-transfer-negotiation"' in modifiers
    assert b'"Name": "attr-wage-negotiation"' in modifiers
    assert modifiers.count(b'"ModAmount": 0.90') == 2

    length_data = data["assets/bin/Data/d062a764541938d4ea0589ed8f071bd8"]
    assert b'"OneYears": 0.5' in length_data, "Native OneYears yazım kusuru kayboldu"
    assert b'"FiveYears": 1.5' in length_data

    status_data = data["assets/bin/Data/7b3db47fa1abe4a4fb28030a16c32de9"]
    assert b'"ID": "KeyPlayer"' in status_data
    assert b'"Backup": 5' in status_data
    assert b'"ID": "Youngster"' in status_data

    wage_trait = data["assets/bin/Data/1cdfc457ed0f567478cc1c3a7dc46c54"]
    renewal_trait = data["assets/bin/Data/a16bfa70e33ea8c429d6455be61b9cfb"]
    signing_trait = data["assets/bin/Data/a6fa163d1b729064184ef557944356b5"]
    assert struct.unpack_from("<i", wage_trait, 332)[0] == 38
    assert struct.unpack_from("<f", wage_trait, 428)[0] == -5.0
    assert struct.unpack_from("<i", renewal_trait, 328)[0] == 36
    assert struct.unpack_from("<f", renewal_trait, 420)[0] == -5.0
    assert struct.unpack_from("<i", signing_trait, 328)[0] == 33
    assert struct.unpack_from("<f", signing_trait, 416)[0] == -5.0
    assert struct.unpack_from("<f", signing_trait, 496)[0] == -10.0


def verify_native() -> None:
    assert sha256(LIBIL2CPP) == EXPECTED_LIB_SHA256, (
        "libil2cpp.so sürümü değişti; adres tabanlı doğrulayıcı FAIL verdi"
    )
    elf = Elf64(LIBIL2CPP)
    assert elf.machine == 183, f"Beklenmeyen ELF makine türü: {elf.machine}"

    # ContractInfo: teklif, eşik, görüşme sayısı, rol ve sonuç alanları.
    assert_instruction(elf, 0x03027CA4, "08 18 40 B9", "Wage +0x18 okuması")
    assert_instruction(elf, 0x03027CA8, "09 24 40 B9", "DemandedWages +0x24 okuması")
    assert_instruction(elf, 0x03027CE8, "08 50 40 79", "NegotiationCount +0x28 okuması")
    assert_instruction(elf, 0x03027CF0, "08 50 00 79", "NegotiationCount +0x28 yazması")
    assert_instruction(elf, 0x03027D04, "09 20 00 B9", "SigningFee +0x20 kopyası")
    assert_instruction(elf, 0x03027D18, "08 2C 00 B9", "SquadStatus +0x2C kopyası")

    # Wage kararı ayrı boolean kapıdır: yarı ücret hakaret, 2 ücret ret, 10 kabul.
    assert_instruction(elf, 0x03027CC0, "40 00 80 52", "Wages sonucu 2")
    assert_instruction(elf, 0x03027CD0, "E0 00 80 52", "Insulted sonucu 7")
    assert_instruction(elf, 0x03027CDC, "48 01 80 52", "Accepted sonucu 10")

    # PlayerPatience başlangıcı, ücret eşikleri ve her gönderimdeki kesin eksiltme sırası.
    assert_instruction(elf, 0x02F9862C, "48 00 80 52", "Kaygılı başlangıç sabrı 2")
    assert_instruction(elf, 0x02F98634, "A9 00 80 52", "Normal başlangıç sabrı 5")
    assert_instruction(elf, 0x02F9865C, "02 10 2D 1E", "Yüzde 75 ücret eşiği")
    assert_instruction(elf, 0x02F98670, "02 10 2C 1E", "Yüzde 50 ücret eşiği")
    assert_instruction(elf, 0x02F98714, "00 10 7E 1E", "Her teklifte sabırdan 1 çıkarma")
    assert_instruction(elf, 0x02F98720, "01 1C 40 FD", "Rol cezası +0x38")
    assert_instruction(elf, 0x02F98728, "01 24 40 FD", "Süre cezası +0x48")
    assert_instruction(elf, 0x02F98730, "01 2C 40 FD", "Ücret cezası +0x58")

    # Final etkileşimli evaluator: ücret -> rol -> süre -> kabul/ret.
    assert_instruction(elf, 0x0303B29C, "09 01 80 52", "OtherOffer sonucu 8")
    assert_instruction(elf, 0x0303B354, "49 01 80 52", "Başlangıç sonucu Accepted 10")
    assert_call(elf, 0x0303B364, 0x03027CA4, "WillAcceptWage kapısı")
    assert_call(elf, 0x0303B39C, 0x02FBC6FC, "WillAcceptSquadStatus kapısı")
    assert_call(elf, 0x0303B3D0, 0x02FC386C, "WillAcceptContractLength kapısı")
    assert_instruction(elf, 0x0303B408, "09 51 40 79", "Ret dalında ikinci NegotiationCount okuması")
    assert_instruction(elf, 0x0303B414, "09 51 00 79", "Ret dalında ikinci NegotiationCount yazması")
    assert_call(elf, 0x0303B438, 0x02FC3F80, "Ret sonrası talebi yeniden üretme")
    assert_call(elf, 0x0303B45C, 0x0303A940, "RejectContractOffer çağrısı")

    # İmza parası: Game.Seed +0x48, oyuncu kimliği, staff 8 ve parasal yuvarlama.
    assert_instruction(elf, 0x02FC34F8, "08 49 40 B9", "Game.Seed +0x48 okuması")
    assert_instruction(elf, 0x02FC34FC, "C9 12 40 B9", "PlayerId +0x10 okuması")
    assert_instruction(elf, 0x02FC3504, "21 01 08 0B", "PlayerId + Game.Seed")
    assert_instruction(elf, 0x02FC3664, "00 01 80 52", "TransferNegotiator kimliği 8")
    assert_call(elf, 0x02FC366C, 0x02F20E54, "TransferNegotiator uygulaması")
    assert_call(elf, 0x02FC367C, 0x031ECC38, "İmza parasının yuvarlanması")
    assert_float64(elf, 0x01332160, 0.05, "Yüksek yenileme alt çarpanı")
    assert_float64(elf, 0x01332950, 0.10, "Yüksek yenileme üst/transfer çarpanı")
    assert_float64(elf, 0x013331E8, 0.025, "Düşük yenileme alt çarpanı")
    assert_float32(elf, 0x0133626C, 0.10, "Genç serbest oyuncu çarpanı")

    # Sadece serbest ve 23 yaş veya altındaki oyuncuya yüzde 20 tazminat.
    assert_instruction(elf, 0x02FC2E18, "08 00 C1 79", "Kulüp kimliği +0x80 okuması")
    assert_instruction(elf, 0x02FC2E30, "1F 5C 00 71", "23 yaş sınırı")
    assert_float32(elf, 0x01335FD4, 0.20, "Genç oyuncu tazminat oranı")
    assert_call(elf, 0x02FC2E5C, 0x031ECC38, "Tazminat yuvarlama çağrısı")

    # Modal sonucu: 10 kabul; 7 veya biten sabır kesin ret; diğer retler görüşmeyi açık bırakır.
    assert_instruction(elf, 0x030E0F50, "3F 28 00 71", "ProcessContractResult Accepted 10 karşılaştırması")
    assert_call(elf, 0x030E0F7C, 0x02F98420, "PatienceHasRunOut kontrolü")
    assert_instruction(elf, 0x030E0F80, "9F 1E 00 71", "Insulted 7 karşılaştırması")


def main() -> int:
    if not LIBIL2CPP.is_file() or not ASSET_PACK.is_file():
        raise SystemExit("Native ikili veya Unity veri paketi eksik")
    verify_native()
    verify_assets()
    print("VERIFY_CONTRACT_NEGOTIATION=PASS")
    print("DOĞRULANAN=contract_info,state_machine,patience,acceptance,signing_fee,traits,staff,assets")
    print("RNG_TOHUMU=PlayerId+Game.Seed")
    print("WAGE_NEGOTIATOR=0.90")
    print("TRAIT_36=-5%;TRAIT_38=-5%")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
