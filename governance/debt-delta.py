#!/usr/bin/env python3
"""Optional local helper to print numeric deltas between two debt-report.json files.

CI prints the same style of deltas from `governance/print-debt-summary.ps1` after
downloading `governance-previous/debt-report.json` on PRs.
"""
import json
import pathlib
import sys

def main():
    cur_path = pathlib.Path("governance/debt-report.json")
    prev_path = pathlib.Path("governance-previous/debt-report.json")
    if not cur_path.is_file():
        print("SKIP: no current governance/debt-report.json")
        return 0
    if not prev_path.is_file():
        print("INFO: no previous debt report (governance-previous/debt-report.json); skipping delta.")
        return 0
    cur = json.loads(cur_path.read_text(encoding="utf-8"))
    prev = json.loads(prev_path.read_text(encoding="utf-8"))
    keys = sorted(set(cur) | set(prev))
    print("=== Debt metric deltas (current - previous) ===")
    for k in keys:
        if k.startswith("_") or k == "lastGeneratedUtc":
            continue
        try:
            a = float(cur.get(k, 0))
            b = float(prev.get(k, 0))
        except (TypeError, ValueError):
            continue
        d = a - b
        if d == 0:
            continue
        print(f"  {k}: {b} -> {a} (delta {d:+g})")
    return 0

if __name__ == "__main__":
    sys.exit(main())
