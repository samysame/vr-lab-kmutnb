"""Manual smoke test: builds a fixed netlist, runs ngspice, and prints
the output. Use this to check that ngspice is correctly installed and
reachable on PATH before running the FastAPI server.

Not a pytest test (no assertions) - run it directly:
    python manual_smoke_test.py
"""

import subprocess
import sys
from pathlib import Path

BASE_DIR = Path(__file__).resolve().parent
NETLIST_PATH = BASE_DIR / "circuit.cir"
OUTPUT_PATH = BASE_DIR / "output.txt"

NGSPICE_SUBPROCESS_KWARGS = {}
if sys.platform == "win32":
    NGSPICE_SUBPROCESS_KWARGS["creationflags"] = subprocess.CREATE_NO_WINDOW

netlist = """Test Circuit
V1 input 0 DC 5
R1 input 0 1000
.op
.print op v(input)
.end
"""

NETLIST_PATH.write_text(netlist)

subprocess.run(
    ["ngspice", "-b", "-o", str(OUTPUT_PATH), str(NETLIST_PATH)],
    **NGSPICE_SUBPROCESS_KWARGS,
)

print(OUTPUT_PATH.read_text())
