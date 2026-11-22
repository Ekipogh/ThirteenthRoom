#!/usr/bin/env bash
set -euo pipefail

if [[ -f .env ]]; then
    set -a
    source ./.env
    set +a
fi

if [[ $# -ne 2 ]]; then
    echo "Usage: $0 <version> <build-number>" >&2
    exit 2
fi

os_type=$(uname -s)
os_directory=""
if [[ $os_type == Linux ]]; then
    os_directory="linux"
elif [[ $os_type == Darwin ]]; then
    os_directory="macos"
else
    echo "Unsupported OS: $os_type" >&2
    exit 1
fi

version=$1
build_number=$2

for value in "$version" "$build_number"; do
    if [[ ! "$value" =~ ^[A-Za-z0-9][A-Za-z0-9._-]*$ ]]; then
        echo "Version and build number must contain only letters, numbers, periods, underscores, and hyphens." >&2
        exit 2
    fi
done

if [[ -z "${NEXUS_URL:-}" ]]; then
    echo "Required environment variable NEXUS_URL is not set." >&2
    exit 1
fi
if [[ -z "${NEXUS_USERNAME:-}" ]]; then
    echo "Required environment variable NEXUS_USERNAME is not set." >&2
    exit 1
fi
if [[ -z "${NEXUS_PASSWORD:-}" ]]; then
    echo "Required environment variable NEXUS_PASSWORD is not set." >&2
    exit 1
fi

build_dir="./ThirteensRoom"
if [[ ! -d "$build_dir" ]]; then
    echo "Build directory not found: $build_dir" >&2
    exit 1
fi
if [[ -z "$(find "$build_dir" -mindepth 1 -print -quit)" ]]; then
    echo "Build directory is empty: $build_dir" >&2
    exit 1
fi

temp_dir=$(mktemp -d)
trap 'rm -rf -- "$temp_dir"' EXIT
archive_path="$temp_dir/ThirteensRoom.zip"

(cd "$build_dir" && zip -qr "$archive_path" .)

nexus_url=${NEXUS_URL%/}
upload_url="$nexus_url/repository/teamcity-raw/thirteenthroom/releases/$os_directory/$version/$build_number/ThirteensRoom.zip"

echo "Uploading build $version ($build_number) to $upload_url"
curl --fail --location --silent --show-error \
    --user "$NEXUS_USERNAME:$NEXUS_PASSWORD" \
    --upload-file "$archive_path" \
    --header "Content-Type: application/zip" \
    "$upload_url"

echo "Build upload completed."
