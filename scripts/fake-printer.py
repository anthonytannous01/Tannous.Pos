#!/usr/bin/env python3
"""
Fake ESC/POS thermal printer.

Listens on TCP 9100 like a real network receipt printer, saves the raw byte stream,
and prints a human-readable rendering of what would have come out on paper.

Used to verify receipt layout (column alignment, paper width, truncation) without
owning a printer. See PRINTING.md.

Usage:
    python scripts/fake-printer.py                 # listen on 9100, keep serving
    python scripts/fake-printer.py --port 9100 --out captures/

Then in the app: Settings -> Receipt Printer -> Connection = LAN,
Host = this machine's LAN IP, Port = 9100, and tap Print Test Receipt.
"""

import argparse
import datetime
import pathlib
import socket
import sys

# ESC/POS commands that take a fixed number of argument bytes after the opcode.
ESC_ARG_LENGTHS = {
    ord('!'): 1, ord('-'): 1, ord('@'): 0, ord('E'): 1, ord('G'): 1,
    ord('J'): 1, ord('R'): 1, ord('a'): 1, ord('d'): 1, ord('t'): 1,
    ord('{'): 1, ord('M'): 1, ord('SP'[0]): 1,
}
GS_ARG_LENGTHS = {
    ord('!'): 1, ord('B'): 1, ord('L'): 2, ord('W'): 2, ord('h'): 1,
    ord('w'): 1, ord('f'): 1, ord('H'): 1,
}


def decode(raw: bytes) -> str:
    """Strip ESC/POS control sequences, keeping the printable text layout intact."""
    out = bytearray()
    i = 0
    n = len(raw)
    while i < n:
        b = raw[i]
        if b == 0x1B and i + 1 < n:  # ESC
            op = raw[i + 1]
            i += 2 + ESC_ARG_LENGTHS.get(op, 0)
            continue
        if b == 0x1D and i + 1 < n:  # GS
            op = raw[i + 1]
            if op == ord('v'):  # raster image: GS v 0 m xL xH yL yH + data
                if i + 8 <= n:
                    xl, xh, yl, yh = raw[i + 4], raw[i + 5], raw[i + 6], raw[i + 7]
                    size = ((xh << 8) | xl) * ((yh << 8) | yl)
                    out += b"[IMAGE]"
                    i += 8 + size
                    continue
            i += 2 + GS_ARG_LENGTHS.get(op, 0)
            continue
        if b in (0x0A, 0x0D) or 0x20 <= b < 0x7F:
            out.append(b)
        i += 1
    return out.decode("ascii", errors="replace")


def render(raw: bytes) -> None:
    text = decode(raw).replace("\r\n", "\n").replace("\r", "\n")
    lines = text.split("\n")
    width = max((len(l) for l in lines), default=0)

    print(f"\n  captured {len(raw)} bytes -> {len(lines)} lines, widest line {width} chars")
    if width in (32, 48):
        print(f"  looks like {'58mm' if width == 32 else '80mm'} paper")
    print("\n  " + "+" + "-" * (width + 2) + "+")
    for line in lines:
        if line == "" and line is lines[-1]:
            continue
        flag = "" if len(line) <= width else "  <-- OVERFLOW"
        print(f"  | {line.ljust(width)} |{flag}")
    print("  " + "+" + "-" * (width + 2) + "+\n")

    overflow = [l for l in lines if len(l) > width]
    if overflow:
        print(f"  !! {len(overflow)} line(s) exceed the paper width\n")


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--port", type=int, default=9100)
    ap.add_argument("--host", default="0.0.0.0")
    ap.add_argument("--out", default="captures")
    args = ap.parse_args()

    outdir = pathlib.Path(args.out)
    outdir.mkdir(parents=True, exist_ok=True)

    srv = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    srv.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    srv.bind((args.host, args.port))
    srv.listen(1)

    # Best-effort report of the address the tablet should point at.
    try:
        probe = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        probe.connect(("8.8.8.8", 80))
        lan_ip = probe.getsockname()[0]
        probe.close()
    except Exception:
        lan_ip = "<this machine's LAN IP>"

    print(f"Fake ESC/POS printer listening on {args.host}:{args.port}")
    print(f"Point the app at  Host = {lan_ip}   Port = {args.port}")
    print("Ctrl+C to stop.\n")

    try:
        while True:
            conn, addr = srv.accept()
            print(f"--- print job from {addr[0]} ---")
            chunks = []
            conn.settimeout(5.0)
            try:
                while True:
                    data = conn.recv(4096)
                    if not data:
                        break
                    chunks.append(data)
            except socket.timeout:
                pass
            finally:
                conn.close()

            raw = b"".join(chunks)
            if not raw:
                print("  (empty job)\n")
                continue

            stamp = datetime.datetime.now().strftime("%Y%m%d-%H%M%S")
            path = outdir / f"receipt-{stamp}.bin"
            path.write_bytes(raw)
            render(raw)
            print(f"  raw bytes saved to {path}\n")
    except KeyboardInterrupt:
        print("\nstopped")
    finally:
        srv.close()
    return 0


if __name__ == "__main__":
    sys.exit(main())
