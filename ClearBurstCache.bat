@echo off
echo Clearing Unity Burst Cache...

REM Clear Burst cache in AppData
if exist "%USERPROFILE%\AppData\Local\Unity\cache\burst" (
    echo Deleting Burst cache folder...
    rmdir /s /q "%USERPROFILE%\AppData\Local\Unity\cache\burst"
    echo Burst cache cleared.
) else (
    echo Burst cache folder not found.
)

REM Clear Library/BurstCache in project
if exist "Library\BurstCache" (
    echo Deleting project Burst cache...
    rmdir /s /q "Library\BurstCache"
    echo Project Burst cache cleared.
)

REM Clear Temp folders
if exist "Temp" (
    echo Deleting Temp folder...
    rmdir /s /q "Temp"
    echo Temp folder cleared.
)

echo.
echo Burst cache clearing complete!
echo Please restart Unity to rebuild the cache.
pause