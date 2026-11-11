#!/usr/bin/env python3
"""Extract matching Method blocks from a Cpp2IL ISIL text file."""

from __future__ import annotations

import argparse
import re
from pathlib import Path


METHOD_START = re.compile(r"^Method:\s+(.*)$")


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("isil_file", type=Path)
    parser.add_argument("name", help="case-insensitive substring of the method signature")
    parser.add_argument("--output", type=Path)
    args = parser.parse_args()

    lines = args.isil_file.read_text(encoding="utf-8", errors="replace").splitlines()
    matches: list[str] = []
    index = 0
    while index < len(lines):
        match = METHOD_START.match(lines[index])
        if not match:
            index += 1
            continue
        end = index + 1
        while end < len(lines) and not METHOD_START.match(lines[end]):
            end += 1
        if args.name.casefold() in match.group(1).casefold():
            matches.append("\n".join(lines[index:end]).rstrip())
        index = end

    if not matches:
        raise SystemExit(f"No method signature contains {args.name!r}")
    output = "\n\n".join(matches) + "\n"
    if args.output:
        args.output.parent.mkdir(parents=True, exist_ok=True)
        args.output.write_text(output, encoding="utf-8")
    print(output, end="")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
