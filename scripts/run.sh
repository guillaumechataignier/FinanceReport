#!/usr/bin/env bash
# Lance FinanceReport en « production locale » : le front compilé est servi par l'API (TS §1.2).
#
#   scripts/run.sh               compile le front, le copie dans l'API, puis démarre sur http://localhost:5080
#   scripts/run.sh --skip-build  démarre avec le front déjà copié
#
# Variables facultatives : Api__Port (5080), Storage__DataPath (./data), Logs__Path (./logs).
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
API_PROJECT="$ROOT/back/src/FinanceReport.Api"
WWWROOT="$API_PROJECT/wwwroot"

if [[ "${1:-}" != "--skip-build" ]]; then
  echo "Compilation du front…"
  (
    cd "$ROOT/front"
    [[ -d node_modules ]] || npm ci --no-audit --no-fund
    npx ng build --configuration production
  )
  rm -rf "$WWWROOT"
  mkdir -p "$WWWROOT"
  cp -R "$ROOT/front/dist/front/browser/." "$WWWROOT/"
fi

export Storage__DataPath="${Storage__DataPath:-$ROOT/data}"
export Logs__Path="${Logs__Path:-$ROOT/logs}"
export ASPNETCORE_ENVIRONMENT="${ASPNETCORE_ENVIRONMENT:-Production}"

echo "FinanceReport : http://localhost:${Api__Port:-5080} (données : $Storage__DataPath)"
exec dotnet run --project "$API_PROJECT" --configuration Release --no-launch-profile
