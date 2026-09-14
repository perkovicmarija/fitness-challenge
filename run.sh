#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")"

if [ -f .env ]; then
    set -a
    . ./.env
    set +a
fi

export Coach__Endpoint="${COACH_ENDPOINT:-}"
export Coach__ApiKey="${COACH_API_KEY:-}"
export Coach__Deployment="${COACH_DEPLOYMENT:-}"

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
