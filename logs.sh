#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")"

case "${1:-full}" in
  off)
    docker compose up -d
    echo "Logging off. The app is still running on http://localhost:8080"
    exit 0
    ;;
  short) filter='Request finished' ;;
  full)  filter='' ;;
  *)
    echo "Usage: ./logs.sh [full|short|off]" >&2
    exit 1
    ;;
esac

export LOG_REQUESTS=Information
export LOG_SQL=Information

docker compose up -d

echo
echo "Watching http://localhost:8080 — open the app, and what it does appears here."
echo "Ctrl+C to stop watching. Then ./logs.sh off to quieten it down again."
echo

if [ -n "$filter" ]; then
    docker compose logs -f --since 1s | grep --line-buffered "$filter"
else
    docker compose logs -f --since 1s
fi
