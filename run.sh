#!/usr/bin/env bash
# Runs the API and the web app together for local development.
#
# The two are separate applications with separate runtimes, so this starts both and shuts both
# down on Ctrl+C. The Angular dev server proxies /api to the API (see web/proxy.conf.json), so
# the browser only ever talks to one origin.
set -euo pipefail

cd "$(dirname "$0")"

API_URL="http://localhost:5032"
WEB_URL="http://localhost:4200"

cleanup() {
    trap - EXIT INT TERM
    kill 0
}
trap cleanup EXIT INT TERM

printf 'API  %s\nWeb  %s\n\n' "$API_URL" "$WEB_URL"

dotnet run --project src/FitnessChallenge.Api --urls "$API_URL" &
npm --prefix web start &

wait
