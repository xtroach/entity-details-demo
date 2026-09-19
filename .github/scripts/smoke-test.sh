#!/usr/bin/env bash
# Smoke-tests a running stack: the API reports ready (database reachable), serves data and completes a
# create/read/delete round trip, and the client serves its host page, falls back to it for client-side
# routes, and has had API_BASE_URL applied by its entrypoint hook. Exits non-zero on the first failed
# check.
#
# Docker Compose (defaults), after `docker compose up -d` from the repository root:
#   bash .github/scripts/smoke-test.sh
# A deployed environment, which isn't seeded:
#   API_URL=https://... CLIENT_URL=https://... EXPECT_SEEDED_DATA=false bash .github/scripts/smoke-test.sh
# Used by CI (.github/workflows/ci.yml) for both.
set -euo pipefail

api_url="${API_URL:-http://localhost:5080}"
client_url="${CLIENT_URL:-http://localhost:8080}"
expect_seeded_data="${EXPECT_SEEDED_DATA:-true}"

fail() {
    echo "FAILED: $1" >&2
    if [ -n "${2:-}" ]; then
        echo "--- response (first 1000 bytes) ---" >&2
        head -c 1000 <<< "$2" >&2
        echo >&2
    fi
    exit 1
}

# Usage: expect <description> <url> <text the response must contain>
# Retries cover containers still starting (or scaling up from zero); after that, an HTTP error status or
# a response without the expected text fails the check. The response is read in full before matching,
# so an early exit from grep can't break the pipe to curl.
expect() {
    local description="$1" url="$2" expected="$3" body
    echo "check: $description"
    body="$(curl --fail --silent --show-error --retry 30 --retry-delay 2 --retry-all-errors "$url")"
    grep -qF -- "$expected" <<< "$body" || fail "$url did not contain: $expected" "$body"
}

# Usage: request <method> <url> [json body]  -> sets $status (HTTP status code) and $response (body).
# Runs in the current shell, not a $(...) subshell, so both variables reach the caller.
# No retries: by the time this runs, the readiness check has passed, and retrying a POST could create a
# duplicate record.
request() {
    local method="$1" url="$2" data="${3:-}" output
    local args=(--silent --show-error --request "$method" --write-out '\n%{http_code}')
    if [ -n "$data" ]; then
        args+=(--header 'Content-Type: application/json' --data "$data")
    fi
    output="$(curl "${args[@]}" "$url")"
    response="${output%$'\n'*}"
    status="${output##*$'\n'}"
}

expect "API is ready (database reachable)" \
    "$api_url/health/ready" 'Healthy'

if [ "$expect_seeded_data" = "true" ]; then
    expect "API returns seeded forecasts" \
        "$api_url/weatherforecast" '"temperatureC"'
fi

# Proves writes, the migrated schema and the database end to end, and leaves no data behind.
echo "check: API creates, reads and deletes a forecast"
request POST "$api_url/weatherforecast" \
    "{\"date\":\"$(date -u +%Y-%m-%d)\",\"temperatureC\":21,\"summary\":\"smoke test\"}"
[ "$status" = 201 ] || fail "POST /weatherforecast returned $status, expected 201" "$response"
id="$(sed -nE 's/.*"id":([0-9]+).*/\1/p' <<< "$response")"
[ -n "$id" ] || fail "POST /weatherforecast returned no id" "$response"
request GET "$api_url/weatherforecast/$id"
[ "$status" = 200 ] || fail "GET /weatherforecast/$id returned $status, expected 200" "$response"
grep -qF '"summary":"smoke test"' <<< "$response" || fail "GET /weatherforecast/$id returned another record" "$response"
request DELETE "$api_url/weatherforecast/$id"
[ "$status" = 204 ] || fail "DELETE /weatherforecast/$id returned $status, expected 204" "$response"
request GET "$api_url/weatherforecast/$id"
[ "$status" = 404 ] || fail "GET /weatherforecast/$id after DELETE returned $status, expected 404" "$response"

expect "client serves the Blazor host page" \
    "$client_url/" '<div id="app">'

expect "client falls back to the host page for the client-side route /weather" \
    "$client_url/weather" '<div id="app">'

expect "client's appsettings.json points at the API (API_BASE_URL applied)" \
    "$client_url/appsettings.json" "\"ApiBaseUrl\": \"$api_url\""

echo "smoke test passed"
