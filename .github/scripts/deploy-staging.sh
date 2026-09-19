#!/usr/bin/env bash
# Deploys one pair of images to the staging environment on Azure, in an order that keeps the running
# version serving until the new one is ready:
#   1. converge the infrastructure (infra/main.bicep) while keeping the currently running images, so
#      configuration and infrastructure changes apply but the running code doesn't change yet;
#   2. apply migrations with the NEW API image (the migration job runs it with --migrate) and stop if
#      they fail, leaving the old version running;
#   3. roll out the new images (each new revision only takes traffic once its readiness probe passes);
#   4. smoke-test the live URLs.
#
# Used by CI's "Deploy to staging" job; runnable locally after `az login` with the same variables.
# Required: AZURE_RESOURCE_GROUP, API_IMAGE, CLIENT_IMAGE (images by digest: name@sha256:...).
# Optional: ENVIRONMENT_NAME (default staging; must match infra/<name>.bicepparam's environmentName).
set -euo pipefail

: "${AZURE_RESOURCE_GROUP:?must be set}" "${API_IMAGE:?must be set}" "${CLIENT_IMAGE:?must be set}"
environment_name="${ENVIRONMENT_NAME:-staging}"
resource_group="$AZURE_RESOURCE_GROUP"

# Names as infra/main.bicep defines them; they're needed before the deployment runs (step 1).
api_app="ca-entitydetails-api-$environment_name"
client_app="ca-entitydetails-client-$environment_name"

# Appends a line to the job summary in CI, or prints it locally.
summary() {
    if [ -n "${GITHUB_STEP_SUMMARY:-}" ]; then echo "$1" >> "$GITHUB_STEP_SUMMARY"; else echo "$1"; fi
}

# Prints the image an app runs now, or nothing if the app doesn't exist yet (first deploy).
current_image() {
    az containerapp show --resource-group "$resource_group" --name "$1" \
        --query 'properties.template.containers[0].image' --output tsv 2>/dev/null || true
}

echo "::group::1. Converge infrastructure (keeping the running images)"
current_api_image="$(current_image "$api_app")"
current_client_image="$(current_image "$client_app")"
echo "running API image:    ${current_api_image:-<none, first deploy>}"
echo "running client image: ${current_client_image:-<none, first deploy>}"
deployment_name="$environment_name-$(date -u +%Y%m%d%H%M%S)"
# Incremental mode (the default, stated explicitly): resources that aren't in the template, such as
# GitHub's deploy identity, must never be deleted. The .bicepparam reads the images from the
# environment, so the running images are passed for this step only.
API_IMAGE="${current_api_image:-$API_IMAGE}" CLIENT_IMAGE="${current_client_image:-$CLIENT_IMAGE}" \
    az deployment group create \
    --resource-group "$resource_group" \
    --name "$deployment_name" \
    --mode Incremental \
    --parameters "infra/$environment_name.bicepparam" \
    --output none
output() {
    az deployment group show --resource-group "$resource_group" --name "$deployment_name" \
        --query "properties.outputs.$1.value" --output tsv
}
api_url="$(output apiUrl)"
client_url="$(output clientUrl)"
migration_job="$(output migrationJobName)"
echo "::endgroup::"

echo "::group::2. Apply migrations with the new API image"
az containerapp job update --resource-group "$resource_group" --name "$migration_job" \
    --image "$API_IMAGE" --output none
execution="$(az containerapp job start --resource-group "$resource_group" --name "$migration_job" \
    --query name --output tsv)"
echo "migration job execution: $execution"
deadline=$((SECONDS + 900))
while :; do
    status="$(az containerapp job execution show --resource-group "$resource_group" --name "$migration_job" \
        --job-execution-name "$execution" --query properties.status --output tsv)"
    echo "status: $status"
    case "$status" in
        Succeeded) break ;;
        Failed | Stopped | Degraded)
            echo "::error::Migration job execution $execution ended as $status; the running version is unchanged."
            az containerapp job execution show --resource-group "$resource_group" --name "$migration_job" \
                --job-execution-name "$execution" --output jsonc || true
            echo "Logs: Log Analytics workspace log-entitydetails-$environment_name, table ContainerAppConsoleLogs_CL, filter ContainerJobName_s == '$migration_job'."
            exit 1
            ;;
    esac
    if [ "$SECONDS" -gt "$deadline" ]; then
        echo "::error::Migration job execution $execution didn't finish within 15 minutes; the running version is unchanged."
        exit 1
    fi
    sleep 10
done
echo "::endgroup::"

echo "::group::3. Roll out the new images"
az containerapp update --resource-group "$resource_group" --name "$api_app" --image "$API_IMAGE" --output none
az containerapp update --resource-group "$resource_group" --name "$client_app" --image "$CLIENT_IMAGE" --output none
echo "::endgroup::"

echo "::group::4. Smoke-test the live URLs"
# Staging runs as "Staging", which isn't seeded; the smoke test's write round trip covers the database.
API_URL="$api_url" CLIENT_URL="$client_url" EXPECT_SEEDED_DATA=false bash .github/scripts/smoke-test.sh
echo "::endgroup::"

if [ -n "${GITHUB_OUTPUT:-}" ]; then
    echo "client-url=$client_url" >> "$GITHUB_OUTPUT"
fi
summary "### Deployed to $environment_name"
summary "- Client: $client_url"
summary "- API: $api_url"
summary "- API image: \`$API_IMAGE\`"
summary "- Client image: \`$CLIENT_IMAGE\`"
summary "- Migration job execution: \`$execution\` (Succeeded)"
