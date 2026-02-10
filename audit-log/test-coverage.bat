@echo off
setlocal enabledelayedexpansion

REM Set paths
set SOLUTION_DIR=%cd%
set REPORT_DIR=%SOLUTION_DIR%/coverage-report
set TESTRESULTS_DIR=%SOLUTION_DIR%/TestResults
REM Clean previous results
echo Cleaning previous test results...
rmdir /s /q "%TESTRESULTS_DIR%"
rmdir /s /q "%REPORT_DIR%"
mkdir "%REPORT_DIR%"
mkdir "%TESTRESULTS_DIR%"

REM Find and run all test projects
echo Running tests with coverage...
dotnet test AuditLog.slnx --configuration: Debug --no-build --collect:"XPlat Code Coverage" --results-directory "%TESTRESULTS_DIR%"

REM Merge and generate report


echo Generating report...
reportgenerator -reports:"%TESTRESULTS_DIR%/**/Coverage/coverage.cobertura.xml" -targetdir:"%REPORT_DIR%" -reporttypes:Html_Dark
echo Coverage report generated at: %REPORT_DIR%
