@echo off
REM ==========================================================================
REM generate_icon.bat
REM
REM End-to-end build for a single character icon:
REM   1) PNG sprite sheet  -> 3 VOX files                  (png4_to_vox.py)
REM   2) each VOX          -> .obj + .mtl + palette .png    (vox_to_obj_exporter.py, --y-up)
REM   3) deploy obj/mtl/png triples for all 3 frames into the Unity Assets folder
REM
REM Usage:
REM     generate_icon <icon_id>
REM Example:
REM     generate_icon 007
REM ==========================================================================

setlocal EnableDelayedExpansion

if "%~1"=="" (
    echo Usage: generate_icon ^<icon_id^>
    echo Example: generate_icon 007
    exit /b 1
)

set "ICON=%~1"
set "ROOT=D:\SourceCode\Git\toneyisnow\windingtale"
set "GEN=%ROOT%\Tools\VOX_Generator\png4_to_vox.py"
set "CONV=%ROOT%\Tools\Vox_to_Obj\vox_to_obj_exporter.py"
set "SRC=%ROOT%\Resources\Original\Icons\%ICON%"
set "REM_OUT=%ROOT%\Resources\Remastered\Icons\%ICON%"
set "UNITY=%ROOT%\WindingTale2\Assets\Resources\Icons\%ICON%"

if not exist "%SRC%" (
    echo ERROR: source folder not found: %SRC%
    exit /b 1
)

echo === [1/3] PNG -^> VOX   ( %SRC%  -^>  %REM_OUT% )
python "%GEN%" "%SRC%" "%REM_OUT%"
if errorlevel 1 (
    echo ERROR: png4_to_vox.py failed
    exit /b !errorlevel!
)

echo.
echo === [2/3] VOX -^> OBJ + MTL + palette PNG   (--y-up)
for %%F in (01 02 03) do (
    echo --- frame %%F ---
    python "%CONV%" "%REM_OUT%\Icon_%ICON%_%%F.vox" --y-up
    if errorlevel 1 (
        echo ERROR: vox_to_obj_exporter.py failed on frame %%F
        exit /b !errorlevel!
    )
)

echo.
echo === [3/3] copy to Unity Assets   ( %UNITY% )
if not exist "%UNITY%" mkdir "%UNITY%"
for %%F in (01 02 03) do (
    for %%E in (obj mtl png) do (
        copy /Y "%REM_OUT%\Icon_%ICON%_%%F.%%E" "%UNITY%\" >nul
        if errorlevel 1 (
            echo ERROR: failed to copy Icon_%ICON%_%%F.%%E
            exit /b !errorlevel!
        )
        echo   + Icon_%ICON%_%%F.%%E
    )
)

echo.
echo Done. icon-%ICON%: 3 VOX in %REM_OUT%, 9 files deployed to %UNITY%
endlocal
