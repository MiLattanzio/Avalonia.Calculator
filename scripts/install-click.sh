#!/usr/bin/env bash
set -euo pipefail
project_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$project_dir"
serial="${1:?Usage: scripts/install-click.sh ADB_SERIAL}"
package_name="$(python3 -c 'import json; m=json.load(open("packaging/click/manifest.json")); print("{name}_{version}_{architecture}.click".format(**m))')"
app_id="$(python3 -c 'import json; m=json.load(open("packaging/click/manifest.json")); print(m["name"]+"_calculator_"+m["version"])')"
remote_home="$(adb -s "$serial" shell 'printf %s "$HOME"' | tr -d '\r')"
uid="$(adb -s "$serial" shell id -u | tr -d '\r')"
[[ "$remote_home" =~ ^/home/[a-zA-Z0-9_-]+$ && "$uid" =~ ^[0-9]+$ ]] || { echo 'Unexpected device session' >&2; exit 1; }
adb -s "$serial" push "artifacts/click/$package_name" "$remote_home/$package_name"
adb -s "$serial" shell "gdbus call --system --dest com.lomiri.click --object-path /com/lomiri/click --method com.lomiri.click.Install '$remote_home/$package_name'"
adb -s "$serial" shell "XDG_RUNTIME_DIR=/run/user/$uid DBUS_SESSION_BUS_ADDRESS=unix:path=/run/user/$uid/bus lomiri-app-launch '$app_id'"
