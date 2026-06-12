# Load environment variables from .env file
if [ -f .env ]; then
    export $(grep -v '^#' .env | xargs)
else
    echo ".env file not found. Make sure the required environment variables are set."
fi

current_version=$(cat artifacts_version.txt | xargs)
echo "Current version: $current_version"

if [ -z "$CACHE_DIR" ]; then
   CACHE_DIR="./tc-cache/artifacts"
fi
cached_version_file="$CACHE_DIR/artifacts_version.txt"
zip_path="$CACHE_DIR/Assets.zip"
extract_dir="./Assets"
required_extracted_paths=(
    "./Assets/Thirdparty"
    "./Assets/Plugins/Demigiant"
)

mkdir -p "$CACHE_DIR"

if [ -f "$cached_version_file" ]; then
    cached_version=$(cat "$cached_version_file" | xargs)
    echo "Cached version: $cached_version"
else
    echo "No cached version found."
    cached_version=""
fi

artifacts_extracted=true
for path in "${required_extracted_paths[@]}"; do
    if [ ! -e "$path" ]; then
        artifacts_extracted=false
        break
    fi
done

if [ "$current_version" = "$cached_version" ] && [ "$artifacts_extracted" = true ]; then
    echo "Artifacts version $current_version already cached and extracted. No need to download."
    exit 0
fi

if [ "$current_version" != "$cached_version" ] || [ ! -f "$zip_path" ]; then
    if [ "$current_version" != "$cached_version" ]; then
        echo "Artifacts changed: cached='$cached_version', current='$current_version'. Downloading..."
    else
        echo "Cached archive missing. Downloading artifacts version $current_version..."
    fi
    curl -L -o "$zip_path" "$NEXUS_URL/repository/teamcity-raw/thirteenthroom/$current_version/Assets.zip"
    if [ $? -ne 0 ]; then
        echo "Failed to download artifacts. Please check the URL and your network connection."
        exit 1
    fi
else
    echo "Versions match, but extracted artifacts are missing. Using cached archive."
fi

unzip -o "$zip_path" -d "$extract_dir"
echo "$current_version" > "$cached_version_file"
