"""FastAPI backend for the VR Circuit Simulator.

Exposes a single /simulate endpoint: given a supply voltage, a current
limit, and a resistance, it builds a SPICE netlist, runs it through
ngspice, and returns the resulting voltage/current/power values.
"""

import re
import subprocess
import sys
from pathlib import Path

from fastapi import FastAPI, HTTPException
from pydantic import BaseModel

app = FastAPI()

# Resolve paths relative to this file, not the process's working
# directory, so it doesn't matter from where the server is launched.
BASE_DIR = Path(__file__).resolve().parent
NETLIST_PATH = BASE_DIR / "circuit.cir"
OUTPUT_PATH = BASE_DIR / "output.txt"

# CREATE_NO_WINDOW only exists on Windows. On macOS/Linux we simply
# don't pass any special subprocess creation flags.
NGSPICE_SUBPROCESS_KWARGS = {}
if sys.platform == "win32":
    NGSPICE_SUBPROCESS_KWARGS["creationflags"] = subprocess.CREATE_NO_WINDOW


class CircuitParams(BaseModel):
    voltage: float
    max_current: float
    resistance: float


def simulate(voltage: float, max_current: float, resistance: float) -> dict:
    # Guard against division by zero on a perfect short circuit.
    safe_resistance = resistance if resistance > 0 else 0.001

    # Lab power supply behavior: Constant Voltage / Constant Current.
    theoretical_current = voltage / safe_resistance

    if theoretical_current <= max_current:
        # Constant Voltage mode (normal operation).
        actual_voltage = voltage
        mode = "CV"
    else:
        # Constant Current mode: the supply protects itself by
        # lowering its output voltage to stay at max_current.
        actual_voltage = max_current * safe_resistance
        mode = "CC"

    netlist = f"""Lab Circuit
V1 input 0 DC {actual_voltage}
R1 input 0 {safe_resistance}
.op
.print op v(input)
.end
"""
    NETLIST_PATH.write_text(netlist)

    try:
        result = subprocess.run(
            ["ngspice", "-b", "-o", str(OUTPUT_PATH), str(NETLIST_PATH)],
            capture_output=True,
            text=True,
            timeout=10,
            **NGSPICE_SUBPROCESS_KWARGS,
        )
    except FileNotFoundError as exc:
        raise HTTPException(
            status_code=500,
            detail="ngspice executable not found. Is it installed and on PATH?",
        ) from exc
    except subprocess.TimeoutExpired as exc:
        raise HTTPException(
            status_code=500,
            detail="ngspice simulation timed out.",
        ) from exc

    if result.returncode != 0:
        raise HTTPException(
            status_code=500,
            detail=f"ngspice exited with an error: {result.stderr.strip()}",
        )

    output = OUTPUT_PATH.read_text()

    current_match = re.search(r"i\s+([\d.e+-]+)", output)
    voltage_match = re.search(r"input\s+([\d.e+-]+)", output)

    if not current_match or not voltage_match:
        raise HTTPException(
            status_code=500,
            detail="Could not parse ngspice output. Check output.txt for details.",
        )

    # SPICE often reports source current as negative; abs() gives a
    # magnitude that's more convenient for the UI.
    current = abs(float(current_match.group(1)))
    voltage_out = float(voltage_match.group(1))

    return {
        "voltage": voltage_out,
        "current": current,
        "resistance": safe_resistance,
        "power": round(voltage_out * current, 6),
        # Exposed so the game engine can show a CV/CC indicator
        # (e.g. a red/green LED on the virtual power supply).
        "mode": mode,
    }


@app.post("/simulate")
def simulate_circuit(params: CircuitParams):
    return simulate(params.voltage, params.max_current, params.resistance)


@app.get("/health")
def health():
    return {"status": "ok"}
