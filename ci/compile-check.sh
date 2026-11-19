#!/usr/bin/env bash
# Compiles Assets/HummingbirdX/Scripts against Unity reference assemblies
# (BepInEx UnityEngine.Modules NuGet package) plus small uGUI API stubs.
# Editor scripts are excluded because UnityEditor.dll is not redistributed.
set -uo pipefail
cd "$(dirname "$0")/CompileCheck"

ok=""
for v in "6000.0.*" "2022.3.*"; do
  if dotnet restore -p:UnityVer="$v" > restore.log 2>&1; then ok="$v"; break; fi
  echo "restore with UnityEngine.Modules $v failed, trying next"
done
if [ -z "$ok" ]; then cat restore.log; echo "::error::Could not restore Unity reference assemblies"; exit 1; fi
echo "Using UnityEngine.Modules $ok"

dotnet build --no-restore -p:UnityVer="$ok" -clp:NoSummary 2>&1 | tee build.log
status=${PIPESTATUS[0]}

# Emit annotations with repo-relative paths (readable through the public checks API).
grep -E "error CS[0-9]+" build.log | sort -u | head -60 | while IFS= read -r line; do
  file=$(echo "$line" | sed -E 's#^(.*Assets/HummingbirdX/[^()]+)\(([0-9]+),([0-9]+)\).*#\1#; s#^.*(Assets/HummingbirdX/)#\1#')
  ln=$(echo "$line" | sed -nE 's#^.*\(([0-9]+),([0-9]+)\).*#\1#p')
  msg=$(echo "$line" | sed -E 's#^.*(error CS[0-9]+: [^\[]*).*#\1#')
  echo "::error file=${file},line=${ln}::${msg}"
done

count=$(grep -cE "error CS[0-9]+" build.log || true)
echo "### Compile check: ${count} error lines" >> "${GITHUB_STEP_SUMMARY:-/dev/null}"
exit $status
