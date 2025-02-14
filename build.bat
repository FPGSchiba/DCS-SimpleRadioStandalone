@echo off

::Command line arguments
set version=%1

:::::::::  File structure settings :::::::::::::::::

::Release
set "releasesFolderName=VCS-SRS-%version%"
set "releasesFolder=%releasesFolderName%"

::Client Release
set clientArchiveName=VCS-SRS-Client-%version%.zip

::Final Release Archive
set "releasesArchiveName=.\VCS-SRS-%version%.zip"

::::::::::: /File structure settings ::::::::::::::::


:: Build

:: Set to quiet, redirected to NUL for batch file testing - change if needed

nuget restore
msbuild -v:q /p:Configuration=Release /p:Platform=x64 /p:SourceLinkCreate=false /p:Version=%version% > NULL
IF NOT errorlevel 0 (
    goto msbuilderror
) 
echo msbuild completed with no error level

:::: Create File Structure  
::
::   .\Vanguard-SRS-%version%\presets        -- Preset manager, etc
::   .\Vanguard-SRS-%version%\radio-client   -- SRS Client
::   

mkdir %releasesFolder%\
::mkdir %releasesFolderName%
echo Created Release Folders

::Cleanup uneeded files
DEL /F .\install-build\DCS-SR-ExternalAudio.exe .\install-build\Installer.exe .\install-build\SRS-AutoUpdater.exe .\install-build\SR-Server.exe
echo Removed unneeded files


:: Move the build into the client fold

XCOPY .\install-build\ %releasesFolder% /Y /q /e
echo Copied Client to Release 


:: Final release archive
tar -acf %releasesArchiveName% %releasesFolderName%
IF NOT errorlevel 0 (
    goto tarerror
)

::Cleanup - when I know what to clean up


echo Finished


goto end

::: Error handling

:msbuilderror

Echo msbuild exited with error level %errorlevel%
goto end

:tarerror

echo Error creating Archive: tar.exe


:end