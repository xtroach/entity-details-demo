#!/usr/bin/env bash
# Smoke-tests the Docker Compose stack after `docker compose up -d` from the repository root: the API
# reports ready (database reachable) and serves seeded data, and the client serves its host page,
# falls back to it for client-side routes, and has had API_BASE_URL applied by its entrypoint hook.
# Used by CI (.github/workflows/ci.yml) and runnable locally with the same command. Exits non-zero on
# the first failed check.
set -euo pipefail

api_url="${API_URL:-http://localhost:5080}"
client_url="${CLIENT_URL:-http://localhost:8080}"

# Usage: expect <description> <url> <text the response must contain>
# Retries cover the containers still starting; after that, an HTTP error status or a response without
# the expected text fails the check. The response is read in full before matching, so an early exit
# from grep can't break the pipe to curl.
expect() {
    local description="$1" url="$2" expected="$3" body
    echo "check: $description"
    body="$(curl --fail --silent --show-error --retry 30 --retry-delay 2 --retry-all-errors "$url")"
    if ! grep -qF -- "$expected" <<< "$body"; then
        echo "FAILED: $url did not contain: $expected" >&2
        echo "--- response (first 1000 bytes) ---" >&2
        head -c 1000 <<< "$body" >&2
        exit 1
    fi
}

expect "API is ready (database reachable)" \
    "$api_url/health/ready" 'Healthy'

expect "API returns seeded forecasts" \
    "$api_url/weatherforecast" '"temperatureC"'

expect "client serves the Blazor host page" \
    "$client_url/" '<div id="app">'

expect "client falls back to the host page for the client-side route /weather" \
    "$client_url/weather" '<div id="app">'

expect "client's appsettings.json points at the API (API_BASE_URL applied)" \
    "$client_url/appsettings.json" "\"ApiBaseUrl\": \"$api_url\""

echo "smoke test passed"
