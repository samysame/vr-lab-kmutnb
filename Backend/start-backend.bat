@echo off
setlocal

REM Always run from this script's own directory, regardless of where
REM it was called from.
set SCRIPT_DIR=%~dp0
cd /d "%SCRIPT_DIR%"

if not exist ".venv\Scripts\activate.bat" (
    echo [ERROR] Virtual environment not found in "%SCRIPT_DIR%.venv".
    echo Run the setup steps in Backend\README.md first:
    echo   python -m venv .venv
    echo   .venv\Scripts\activate.bat
    echo   pip install -r requirements.txt
    exit /b 1
)

call ".venv\Scripts\activate.bat"

where ngspice >nul 2>nul
if errorlevel 1 (
    echo [WARNING] ngspice was not found on PATH. The /simulate endpoint will fail until it is installed.
    echo See Backend\README.md for installation instructions.
)

echo Starting backend on http://127.0.0.1:8000 ...
uvicorn main:app --reload --port 8000

endlocal
