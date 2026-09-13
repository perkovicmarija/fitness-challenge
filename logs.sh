#!/usr/bin/env bash
# Shows what the app is doing, one request at a time.
#
#   ./logs.sh          every request, with the SQL it ran
#   ./logs.sh short    one line per request: path, status, how long it took
#   ./logs.sh off      back to quiet
#
# Request and SQL logging are off by default, so this switches them on, rebuilds the container
# with them, and follows the output. Ctrl+C stops watching; the app keeps running.
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
