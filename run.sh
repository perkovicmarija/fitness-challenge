#!/usr/bin/env bash
# Runs the API and the web app together for local development.
#
# The two are separate applications with separate runtimes, so this starts both and shuts both
# down on Ctrl+C. The Angular dev server proxies /api to the API (see web/proxy.conf.json), so
# the browser only ever talks to one origin.
set -euo pipefail

cd "$(dirname "$0")"

# The Azure credentials live in one .env file, read here and by docker compose. Without it the
# app runs exactly the same and only the AI chat is switched off. See .env.example.
if [ -f .env ]; then
    set -a
    . ./.env
    set +a
fi

export Coach__Endpoint="${COACH_ENDPOINT:-}"
export Coach__ApiKey="${COACH_API_KEY:-}"
export Coach__Deployment="${COACH_DEPLOYMENT:-}"

# Demo data, written once into an empty database. An existing database is left alone.
export Seed=true

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
