# VR Circuit Simulator — Backend

This is the simulation backend for the VR electronics lab project. It's a small
FastAPI server that receives circuit parameters (voltage, current limit,
resistance) from the game engine over HTTP, builds a SPICE netlist, runs it
through **ngspice**, and returns the computed voltage/current/power.

The game engine (Unity/Unreal) is a separate application. This backend must be
running locally before the game is launched, since the engine calls it over
`http://127.0.0.1:8000`.

## Prerequisites

- **Python 3.11 or newer** (any recent 3.x should work; avoid mixing Python
  versions between contributors on the same machine to prevent subtle
  dependency issues).
- **ngspice**, installed separately and available on your system `PATH`. This
  backend shells out to the `ngspice` command — it is not a Python package.

### Installing ngspice

- **Windows**: download the ngspice Windows build from
  [ngspice.sourceforge.io](https://ngspice.sourceforge.io/download.html),
  extract it, and add its `bin/` (or `Spice64/bin/`) folder to your `PATH`
  environment variable.
- **macOS**: `brew install ngspice`
- **Linux (Debian/Ubuntu)**: `sudo apt install ngspice`

Verify the install by opening a terminal and running:
```bash
ngspice -v
```
If you see a version number, you're good. If you get a "command not found"
error, `ngspice` is not on your `PATH` — fix that before continuing.

## Setup

1. **Clone the repository** and navigate to this backend folder.

2. **Create a virtual environment** (do this once per machine — never commit
   this folder):
   ```bash
   python -m venv .venv
   ```

3. **Activate it.**
   - Windows (PowerShell):
     ```powershell
     .venv\Scripts\Activate.ps1
     ```
   - Windows (cmd.exe):
     ```cmd
     .venv\Scripts\activate.bat
     ```
   - macOS / Linux:
     ```bash
     source .venv/bin/activate
     ```
   Your terminal prompt should now be prefixed with `(.venv)`.

   > When you're done working on the backend, run When you're done working on the backend, run ````bash deactivate``` to exit the virtual environment and return to your 
   > system's default Python.

4. **Install the Python dependencies:**
   ```bash
   pip install -r requirements.txt
   ```

## Running the server

**Option 1 — provided scripts (recommended).** These activate the virtual
environment, warn you if ngspice isn't on `PATH`, and start the server, so you
don't have to repeat the manual steps every session:

- **Windows**: `start-backend.bat`
- **macOS / Linux**: `./start-backend.sh`

  On macOS/Linux, the executable bit needs to be set once after cloning (Git
  doesn't always preserve it across platforms):
  ```bash
  chmod +x start-backend.sh
  git update-index --chmod=+x start-backend.sh
  ```

**Option 2 — manual.** With the virtual environment active:
```bash
uvicorn main:app --reload --port 8000
```
The `--reload` flag restarts the server automatically when you edit the code —
useful in development, safe to drop for a more "production-like" run.

## Verifying it works

1. **Health check** — open [http://127.0.0.1:8000/health](http://127.0.0.1:8000/health)
   in a browser, or run:
   ```bash
   curl http://127.0.0.1:8000/health
   ```
   Expected response: `{"status":"ok"}`

2. **ngspice smoke test** — run the standalone script to confirm ngspice is
   correctly wired up, independently of FastAPI:
   ```bash
   python manual_smoke_test.py
   ```
   This should print an ngspice operating-point result to the terminal.

3. **Full simulate endpoint** — with the server running, in another terminal:
   ```bash
   curl -X POST http://127.0.0.1:8000/simulate \
     -H "Content-Type: application/json" \
     -d "{\"voltage\": 5, \"max_current\": 1, \"resistance\": 1000}"
   ```
   Expected response (approximately):
   ```json
   {"voltage": 5.0, "current": 0.005, "resistance": 1000.0, "power": 0.025, "mode": "CV"}
   ```

## Project structure

```
.
├── main.py                 # FastAPI app - the /simulate and /health endpoints
├── manual_smoke_test.py     # Standalone script to sanity-check the ngspice install
├── start-backend.bat        # Launch script (Windows)
├── start-backend.sh         # Launch script (macOS/Linux)
├── requirements.txt         # Python dependencies
├── .gitignore
├── circuit.cir               # generated at runtime - not committed
└── output.txt                # generated at runtime - not committed
```

## Troubleshooting

| Symptom | Likely cause |
|---|---|
| `ngspice executable not found` (500 error) | ngspice isn't installed, or isn't on your `PATH`. Re-check the install step above and restart your terminal after adding it to `PATH`. |
| Server won't start / import errors | Virtual environment isn't activated, or dependencies weren't installed. Re-run `pip install -r requirements.txt` with the venv active. |
| Game engine can't reach the backend | Confirm the server is running (`/health` check above) and that nothing else is using port 8000. |
| Every simulation returns `"mode": "CC"` unexpectedly | Check the `max_current` value you're sending — the supply switches to Constant Current mode whenever the theoretical current (`voltage / resistance`) exceeds it. |

## Notes for contributors

- Do not commit `.venv/`, `.conda/`, or any virtual environment folder —
  they're machine-specific and can be very large. Only `requirements.txt`
  should be versioned; everyone recreates their own environment locally.
- `circuit.cir` and `output.txt` are regenerated on every `/simulate` call —
  they're build artifacts, not source files.
