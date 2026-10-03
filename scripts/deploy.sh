#!/usr/bin/env bash
# Release 构建并部署到 RimWorld Mods；不复制 RimTalk、Scriban、Harmony 或游戏程序集。
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
configuration="${CONFIGURATION:-Release}"
rimworld="${RIMWORLD_DIR:-${RimWorldDir:-/mnt/e/Apps/Steam/steamapps/common/RimWorld}}"
target="$rimworld/Mods/AdvancedRimTalk"
build_only=0
if [[ $# -gt 0 ]]; then
  if [[ $# -ne 1 || "$1" != "--build-only" ]]; then
    echo "Usage: $0 [--build-only]" >&2
    exit 1
  fi
  build_only=1
fi

if [[ ! -f "$rimworld/RimWorldWin64_Data/Managed/Assembly-CSharp.dll" ]]; then
  echo "RimWorld managed assemblies not found: $rimworld" >&2
  exit 1
fi

check_game_stopped() {
  if command -v powershell.exe >/dev/null 2>&1; then
    local count
    count="$(powershell.exe -NoProfile -Command '$ErrorActionPreference = "Stop"; @(Get-Process -Name RimWorldWin64,RimWorldWin64Steam,RimWorldWin,RimWorld -ErrorAction SilentlyContinue).Count' | tr -d '\r')"
    if [[ "$count" != "0" ]]; then
      echo "Exit RimWorld before deploying." >&2
      exit 1
    fi
  elif pgrep -x 'RimWorld|RimWorldLinux|RimWorldWin64|RimWorldWin64Steam|RimWorldWin' >/dev/null; then
    echo "Exit RimWorld before deploying." >&2
    exit 1
  fi
}

if [[ "$build_only" == 0 ]]; then
  check_game_stopped
fi

dotnet build "$root/AdvancedRimTalk.csproj" -c "$configuration" -p:RimWorldDir="$rimworld" --nologo
if [[ "$build_only" == 1 ]]; then
  exit 0
fi
check_game_stopped

python3 - "$root" "$target" <<'PY'
import hashlib
import shutil
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

root = Path(sys.argv[1]).resolve()
target = Path(sys.argv[2])
package_id = "advancedrimtalk.prompt"
folders = ("About", "Languages", "docs")
files = ("README.md", "LICENSE")
assembly = root / "tmp/build/AdvancedRimTalk.dll"
pdb = assembly.with_suffix(".pdb")


def check_identity(about):
    if ET.parse(about).getroot().findtext("packageId") != package_id:
        raise SystemExit(f"Unexpected mod identity: {about}")


check_identity(root / "About/About.xml")
if target.is_symlink():
    raise SystemExit(f"Target is a symlink: {target}")
if target.exists():
    if not target.is_dir():
        raise SystemExit(f"Target is not a directory: {target}")
    about = target / "About/About.xml"
    if about.is_file():
        check_identity(about)
    elif any(target.iterdir()):
        raise SystemExit(f"Non-empty target has no mod identity: {target}")
if target.resolve() == root:
    raise SystemExit("Deployment target must not be the source repository.")

sources = [(assembly, Path("Assemblies/AdvancedRimTalk.dll"))]
if pdb.is_file():
    sources.append((pdb, Path("Assemblies/AdvancedRimTalk.pdb")))
for folder in folders:
    path = root / folder
    if not path.is_dir():
        raise SystemExit(f"Required source directory not found: {path}")
    sources.extend((item, item.relative_to(root)) for item in sorted(path.rglob("*")) if item.is_file())
sources.extend((root / file, Path(file)) for file in files)

# Validate the complete manifest before writing into the game directory.
for source, relative in sources:
    if not source.is_file():
        raise SystemExit(f"Required source file not found: {source}")
    destination = target / relative
    if destination.resolve() != target.resolve() / relative:
        raise SystemExit(f"Refusing to deploy through a symlink: {destination}")

for source, relative in sources:
    destination = target / relative
    destination.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(source, destination)
    if hashlib.sha256(source.read_bytes()).digest() != hashlib.sha256(destination.read_bytes()).digest():
        raise SystemExit(f"Hash mismatch: {relative}")

print(f"Deployed and SHA-256 verified {len(sources)} files: {target}")
PY
