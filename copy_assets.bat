assets_path=%1

if "%assets_path%"=="" (
    echo Please provide the path to the assets folder as an argument.
    echo Usage: copy_assets.bat [path_to_assets]
    exit /b 1
)

rem Unzip into the Assets folder
echo Unzipping assets from %assets_path% to Assets folder...
powershell -Command "Expand-Archive -Path '%assets_path%' -DestinationPath 'Assets' -Force"