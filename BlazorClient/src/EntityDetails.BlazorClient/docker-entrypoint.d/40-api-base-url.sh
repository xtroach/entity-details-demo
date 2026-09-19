#!/bin/sh
# Runs from the nginx image's entrypoint before nginx starts. When API_BASE_URL is set, rewrites the
# appsettings.json the browser downloads at startup so one image can target any API address.
set -eu

settings=/usr/share/nginx/html/appsettings.json

if [ -z "${API_BASE_URL:-}" ]; then
    echo "$0: API_BASE_URL not set; keeping the published ApiBaseUrl"
    exit 0
fi

# Escape backslashes and double quotes so the value stays a valid JSON string.
escaped=$(printf '%s' "$API_BASE_URL" | sed -e 's/\\/\\\\/g' -e 's/"/\\"/g')
printf '{\n  "ApiBaseUrl": "%s"\n}\n' "$escaped" > "$settings"

# Drop the precompressed copies written at publish time so gzip_static can't serve the old value.
rm -f "$settings.gz" "$settings.br"

echo "$0: ApiBaseUrl set to $API_BASE_URL"
