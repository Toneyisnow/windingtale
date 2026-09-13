@echo off
REM ==========================================================================
REM generate_icon.bat
REM
REM End-to-end build for one (or all) character icon(s):
REM   1) PNG sprite sheet  -> 3 VOX files                  (png4_to_vox.py)
REM   2) each VOX          -> .obj + .mtl + palette .png    (vox_to_obj_exporter.py)
REM                          plus a rounded, smooth-shaded copy in smoothed\
REM   3) deploy the smoothed obj/mtl/png triples into the Unity Assets folder
REM
REM Usage:
REM     generate_icon <icon_id>      -- build a single icon, e.g. 007
REM     generate_icon all            -- iterate every 3-digit folder under
REM                                     Resources\Original\Icons and build each
REM ==========================================================================

setlocal EnableDelayedExpansion

if "%~1"=="" (
    echo Usage: generate_icon ^<icon_id^>     or     generate_icon all
    exit /b 1
)

set "ROOT=D:\SourceCode\Git\toneyisnow\windingtale"

REM ----------------------------------------------------------------------
REM ALL MODE: iterate every 3-digit folder under Original\Icons and recurse
REM ----------------------------------------------------------------------
if /I "%~1"=="all" (
    set "ORIG=%ROOT%\Resources\Original\Icons"
    if not exist "!ORIG!" (
        echo ERROR: Original icons folder not found: !ORIG!
        exit /b 1
    )
    set "BUILT="
    set "FAILED="
    set "SKIPPED="
    for /d %%D in ("!ORIG!\*") do (
        set "NAME=%%~nxD"
        REM only build folders whose name is exactly three digits
        echo !NAME!| findstr /r /x "[0-9][0-9][0-9]" >nul
        if !errorlevel! equ 0 (
            echo.
            echo ============================================================
            echo Building icon-!NAME!
            echo ============================================================
            call "%~f0" !NAME!
            if !errorlevel! neq 0 (
                set "FAILED=!FAILED! !NAME!"
            ) else (
                set "BUILT=!BUILT! !NAME!"
            )
        ) else (
            set "SKIPPED=!SKIPPED! !NAME!"
        )
    )
    echo.
    echo ============================================================
    echo Summary
    echo ============================================================
    if defined BUILT   echo Built  :!BUILT!
    if defined SKIPPED echo Skipped:!SKIPPED!   ^(non 3-digit folders^)
    if defined FAILED (
        echo Failed :!FAILED!
        exit /b 1
    )
    exit /b 0
)

REM ----------------------------------------------------------------------
REM SINGLE ICON MODE
REM ----------------------------------------------------------------------
set "ICON=%~1"
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
REM Two exports per frame. The plain one lands next to the .vox and is the
REM original hard-edged, flat-shaded model, kept as it always was. The
REM --round one goes into a smoothed\ subfolder: rounded outer edges
REM and smooth vertex normals (see Tools\Vox_to_Obj\rounded_mesh.py).
REM GameMap loads smoothed\ when it is there and falls back to the
REM original when it is not, so both stay valid.
echo === [2/3] VOX -^> OBJ + MTL + palette PNG   (original, then smoothed)
for %%F in (01 02 03) do (
    echo --- frame %%F ---
    python "%CONV%" "%REM_OUT%\Icon_%ICON%_%%F.vox" --y-up
    if errorlevel 1 (
        echo ERROR: vox_to_obj_exporter.py failed on frame %%F
        exit /b !errorlevel!
    )
    python "%CONV%" "%REM_OUT%\Icon_%ICON%_%%F.vox" --y-up --round --out-dir smoothed
    if errorlevel 1 (
        echo ERROR: vox_to_obj_exporter.py --round failed on frame %%F
        exit /b !errorlevel!
    )
)

echo.
echo === [3/3] copy to Unity Assets   ( %UNITY% )
REM Only the version the game actually loads goes into the Unity project:
REM the smoothed one. The hard-edged original stays in %REM_OUT% next to
REM the .vox, so it is still there to compare against or to fall back to.
if not exist "%UNITY%" mkdir "%UNITY%"
for %%F in (01 02 03) do (
    for %%E in (obj mtl png) do (
        copy /Y "%REM_OUT%\smoothed\Icon_%ICON%_%%F.%%E" "%UNITY%\" >nul
        if errorlevel 1 (
            echo ERROR: failed to copy Icon_%ICON%_%%F.%%E
            exit /b !errorlevel!
        )
        echo   + Icon_%ICON%_%%F.%%E
    )
)

echo.
echo Done. icon-%ICON%: 3 VOX + 2 OBJ sets in %REM_OUT%, 9 files deployed to %UNITY%
endlocal
