#!/usr/bin/env python3
"""Create a deterministic inventory for the SM25 research workspace."""

from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path


EXPECTED = (
    "original/base.apk",
    "original/split_config.arm64_v8a.apk",
    "original/split_UnityDataAssetPack.apk",
    "il2cpp/global-metadata.dat",
    "il2cpp/libil2cpp.so",
    "isil/IsilDump/Assembly-CSharp/PlayerData.txt",
    "call_analysis/DiffableCs/Assembly-CSharp/PlayerData.cs",
)


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("root", nargs="?", type=Path, default=Path(__file__).resolve().parent.parent)
    parser.add_argument("--hash", action="store_true", help="also SHA-256 hash expected critical files")
    parser.add_argument("--json", action="store_true")
    args = parser.parse_args()
    root = args.root.resolve()

    areas = []
    for directory in sorted((item for item in root.iterdir() if item.is_dir()), key=lambda item: item.name.casefold()):
        files = [path for path in directory.rglob("*") if path.is_file()]
        areas.append({"name": directory.name, "files": len(files), "bytes": sum(path.stat().st_size for path in files)})

    critical = []
    missing = []
    for relative in EXPECTED:
        path = root / relative
        if not path.is_file():
            missing.append(relative)
            continue
        item: dict[str, str | int] = {"path": relative, "bytes": path.stat().st_size}
        if args.hash:
            item["sha256"] = sha256(path)
        critical.append(item)
    result = {"root": str(root), "areas": areas, "critical": critical, "missing": missing}

    if args.json:
        print(json.dumps(result, indent=2))
    else:
        print(f"ROOT={root}")
        for area in areas:
            print(f"AREA {area['name']}: files={area['files']} bytes={area['bytes']}")
        for item in critical:
            suffix = f" sha256={item['sha256']}" if "sha256" in item else ""
            print(f"OK {item['path']}: bytes={item['bytes']}{suffix}")
        for relative in missing:
            print(f"MISSING {relative}")
    return 1 if missing else 0


if __name__ == "__main__":
    raise SystemExit(main())
