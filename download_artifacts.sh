# Load environment variables from .env file
if [ -f .env ]; then
    export $(grep -v '^#' .env | xargs)
else
    echo ".env file not found. Make sure the required environment variables are set."
fi

current_version=$(cat artifacts_version.txt)
echo "Current version: $current_version"

if [ -z "$CACHE_DIR" ]; then
   CACHE_DIR="./tc-cache/artifacts"
fi
cached_version_file="$CACHE_DIR/artifacts_version.txt"
zip_path="$CACHE_DIR/Assets.zip"
extrac_dir="./Assets"

mkdir -p "$CACHE_DIR"

if [ -f "$cached_version_file" ]; then
    cached_version=$(cat "$cached_version_file")
    echo "Cached version: $cached_version"
else
    echo "No cached version found."
    cached_version=""
fi

if [ "$current_version" != "$cached_version" ]; then
    echo "Versions differ. Downloading new artifacts..."
    curl -L -o "$zip_path" "$NEXUS_URL/repository/teamcity-raw/thirteenthroom/$current_version/Assets.zip"
    if [ $? -ne 0 ]; then
        echo "Failed to download artifacts. Please check the URL and your network connection."
        exit 1
    fi
    unzip -o "$zip_path" -d "$extrac_dir"
    echo "$current_version" > "$cached_version_file"
else
    echo "Versions match. No need to download."
fi