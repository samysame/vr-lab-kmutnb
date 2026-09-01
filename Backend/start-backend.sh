#!/usr/bin/env bash
set -e

# Always run from this script's own directory, regardless of where it
# was called from.
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

if [ ! -f ".venv/bin/activate" ]; then
    echo "[ERROR] Virtual environment not found in $SCRIPT_DIR/.venv"
    echo "Run the setup steps in Backend/README.md first:"
    echo "  python -m venv .venv"
    echo "  source .venv/bin/activate"
    echo "  pip install -r requirements.txt"
    exit 1
fi

source .venv/bin/activate

if ! command -v ngspice >/dev/null 2>&1; then
    echo "[WARNING] ngspice was not found on PATH. The /simulate endpoint will fail until it is installed."
    echo "See Backend/README.md for installation instructions."
fi

echo "Starting backend on http://127.0.0.1:8000 ..."
uvicorn main:app --reload --port 8000
