#!/usr/bin/env python3
"""Import MIT-licensed Uno resources without requiring Uno at build/runtime."""
import json
from pathlib import Path
import sys
import xml.etree.ElementTree as ET
root = Path(__file__).resolve().parents[1]
source = Path(sys.argv[1]) if len(sys.argv) > 1 else root.parent / 'Uno.Calculator'
source = source / 'src/Calculator.Shared/Resources'
target = root / 'src/Calculator/Localization/Resources'
target.mkdir(parents=True, exist_ok=True)
for directory in sorted(source.iterdir()):
    if not (directory / 'Resources.resw').exists():
        continue
    data = {}
    for name, file in [('Strings', 'Resources.resw'), ('Engine', 'CEngineStrings.resw')]:
        data[name] = {node.attrib['name']: node.findtext('value', '') for node in ET.parse(directory / file).findall('data')}
    (target / (directory.name + '.json')).write_text(json.dumps(data, ensure_ascii=False, indent=2, sort_keys=True) + '\n')
print(f'Imported {len(list(target.glob("*.json")))} Uno locales')
