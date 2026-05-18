@echo off
REM ==========================================================================
REM generate_tai.bat
REM
REM End-to-end build for a single (or all) circular battle platform(s):
REM   1) Tai-XX.png        -> Tai_XX.vox                  (tai_to_vox.py)
REM   2) Tai_XX.vox        -> .obj + .mtl + palette .png   (vox_to_obj_exporter.py, --y-up)
REM   3) deploy obj/mtl/png triple to the Unity Assets folder
REM
REM Usage:
REM     generate_tai <id>            -- build one platform, e.g. 01, 99
REM     generate_tai all             -- iterate every Tai-NN.png in
REM                                     Resources\Original\Tais  (2-digit NN)
REM ==========================================================================

setlocal EnableDelayedExpansion

if "%~1"=="" (
    echo Usage: generate_tai ^<id^>     or     generate_tai all
    exit /b 1
)

set "ROOT=D:\SourceCode\Git\toneyisnow\windingtale"

REM ----------------------------------------------------------------------
REM ALL MODE: iterate every Tai-NN.png with 2-digit NN
REM ----------------------------------------------------------------------
if /I "%~1"=="all" (
    set "ORIG=%ROOT%\Resources\Original\Tais"
    if not exist "!ORIG!" (
        echo ERROR: Original Tais folder not found: !ORIG!
        exit /b 1
    )
    set "BUILT="
    set "FAILED="
    set "SKIPPED="
    for %%F in ("!ORIG!\Tai-*.png") do (
        set "BASE=%%~nF"
        REM strip "Tai-" prefix -> the ID
        set "ID=!BASE:Tai-=!"
        REM Only build IDs that are exactly two digits
        echo !ID!| findstr /r /x "[0-9][0-9]" >nul
        if !errorlevel! equ 0 (
            echo.
            echo ============================================================
            echo Building Tai-!ID!
            echo ============================================================
            call "%~f0" !ID!
            if !errorlevel! neq 0 (
                set "FAILED=!FAILED! !ID!"
            ) else (
                set "BUILT=!BUILT! !ID!"
            )
        ) else (
            set "SKIPPED=!SKIPPED! !ID!"
        )
    )
    echo.
    echo ============================================================
    echo Summary
    echo ============================================================
    if defined BUILT   echo Built  :!BUILT!
    if defined SKIPPED echo Skipped:!SKIPPED!   ^(non 2-digit IDs^)
    if defined FAILED (
        echo Failed :!FAILED!
        exit /b 1
    )
    exit /b 0
)

REM ----------------------------------------------------------------------
REM SINGLE PLATFORM MODE
REM ----------------------------------------------------------------------
set "ID=%~1"
set "GEN=%ROOT%\Tools\VOX_Generator\tai_to_vox.py"
set "CONV=%ROOT%\Tools\Vox_to_Obj\vox_to_obj_exporter.py"
set "SRC=%ROOT%\Resources\Original\Tais\Tai-%ID%.png"
set "REM_OUT_DIR=%ROOT%\Resources\Remastered\Tais"
set "VOX_PATH=%REM_OUT_DIR%\Tai_%ID%.vox"
REM Per-id subfolder under Tais so each platform lives in its own folder.
set "UNITY=%ROOT%\WindingTale2\Assets\Resources\Tais\%ID%"

if not exist "%SRC%" (
    echo ERROR: source not found: %SRC%
    exit /b 1
)

echo === [1/3] PNG -^> VOX   ( %SRC% )
python "%GEN%" %ID%
if errorlevel 1 (
    echo ERROR: tai_to_vox.py failed
    exit /b !errorlevel!
)

echo.
echo === [2/3] VOX -^> OBJ + MTL + palette PNG   (--y-up)
python "%CONV%" "%VOX_PATH%" --y-up
if errorlevel 1 (
    echo ERROR: vox_to_obj_exporter.py failed
    exit /b !errorlevel!
)

echo.
echo === [3/3] copy to Unity Assets   ( %UNITY% )
if not exist "%UNITY%" mkdir "%UNITY%"
for %%E in (obj mtl png) do (
    copy /Y "%REM_OUT_DIR%\Tai_%ID%.%%E" "%UNITY%\" >nul
    if errorlevel 1 (
        echo ERROR: failed to copy Tai_%ID%.%%E
        exit /b !errorlevel!
    )
    echo   + Tai_%ID%.%%E
)

echo.
echo Done. Tai-%ID%: VOX in %REM_OUT_DIR%, 3 files deployed to %UNITY%
endlocal
