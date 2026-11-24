#!/usr/bin/env bash
set -euo pipefail
project_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$project_dir"
rid="${1:-linux-x64}"
case "$rid" in
  linux-x64) compiler="${CXX:-g++}" ;;
  linux-arm64) compiler="${CXX:-aarch64-linux-gnu-g++}" ;;
  *) echo "Unsupported native target: $rid" >&2; exit 1 ;;
esac
output="artifacts/native/$rid/libCalculatorEngine.so"
mkdir -p "${output%/*}"
if [[ -f "$output" ]] && ! find native scripts/build-native.sh -type f -newer "$output" -print -quit | read -r _; then exit 0; fi
"$compiler" -std=c++17 -O2 -DNDEBUG -fPIC -shared -pthread \
  -include native/CalcManager/pch.h -I native/CalcManager \
  native/CalcManager/CEngine/*.cpp native/CalcManager/Ratpack/*.cpp \
  native/CalcManager/CalculatorManager.cpp native/CalcManager/CalculatorHistory.cpp \
  native/CalcManager/ExpressionCommand.cpp native/CalculatorBridge.cpp \
  -Wl,-z,defs -o "$output.tmp"
mv "$output.tmp" "$output"
