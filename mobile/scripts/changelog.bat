@echo off
REM Changelog generator for Tannous POS Android App (Windows version)
REM Generates release notes from conventional commits since last tag

echo Generating changelog for Tannous POS...

REM Get the last tag
for /f "delims=" %%i in ('git describe --tags --abbrev^=0 2^>nul') do set LAST_TAG=%%i
if "%LAST_TAG%"=="" set LAST_TAG=v0.0.0
echo Last tag: %LAST_TAG%

REM Create ci directory if it doesn't exist
if not exist "ci" mkdir ci

REM Get commits since last tag
for /f "delims=" %%i in ('git log --pretty^=format:"%%s" %LAST_TAG%..HEAD 2^>nul') do set COMMITS=%%i

if "%COMMITS%"=="" (
    echo No new commits since last tag. Using fallback changelog.
    (
        echo Release Notes - Tannous POS
        echo.
        echo No new commits since %LAST_TAG%
        echo.
        echo This is an automated release with no new features or fixes.
    ) > ci\release-notes.txt
) else (
    echo Found commits since %LAST_TAG%
    
    REM Initialize changelog
    (
        echo Release Notes - Tannous POS
        echo.
        echo Version: %LAST_TAG:~1%
        echo Release Date: %date% %time%
        echo Commit Range: %LAST_TAG%..HEAD
        echo.
    ) > ci\release-notes.txt
    
    echo Changelog generated successfully!
)

REM Display the generated changelog
echo Generated changelog:
echo ----------------------------------------
type ci\release-notes.txt
echo ----------------------------------------

echo Changelog saved to: ci\release-notes.txt
