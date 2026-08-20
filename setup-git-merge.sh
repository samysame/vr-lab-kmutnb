#!/bin/bash
# Configure UnityYAMLMerge as the local Git merge driver for this repo.
# To be run ONCE after cloning the repo (from the repo root).

set -e

echo "Finding UnityYAMLMerge..."

UNITY_MERGE=""

# --- macOS ---
if [[ "$OSTYPE" == "darwin"* ]]; then
    UNITY_MERGE=$(find /Applications/Unity/Hub/Editor -maxdepth 4 -name "UnityYAMLMerge" 2>/dev/null | sort -V | tail -n 1)

# --- Linux ---
elif [[ "$OSTYPE" == "linux-gnu"* ]]; then
    UNITY_MERGE=$(find "$HOME/Unity/Hub/Editor" -maxdepth 4 -name "UnityYAMLMerge" 2>/dev/null | sort -V | tail -n 1)

# --- Windows (Git Bash / MSYS) ---
elif [[ "$OSTYPE" == "msys" || "$OSTYPE" == "cygwin" ]]; then
    UNITY_MERGE=$(find "/c/Program Files/Unity/Hub/Editor" -maxdepth 5 -iname "UnityYAMLMerge.exe" 2>/dev/null | sort -V | tail -n 1)
fi

if [[ -z "$UNITY_MERGE" ]]; then
    echo "UnityYAMLMerge not found automatically."
    read -p "Enter the full path to UnityYAMLMerge (or .exe) : " UNITY_MERGE
fi

if [[ ! -f "$UNITY_MERGE" ]]; then
    echo "Error : file not found at location '$UNITY_MERGE'"
    exit 1
fi

echo "UnityYAMLMerge found : $UNITY_MERGE"

git config --local merge.unityyamlmerge.name "Unity YAML Merge"
git config --local merge.unityyamlmerge.driver "'$UNITY_MERGE' merge -p %O %B %A %A"

echo ""
echo "Configuration completed successfully."
echo "Verification :"
git config --local --get merge.unityyamlmerge.driver
