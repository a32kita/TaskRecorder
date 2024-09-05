@echo off

REM Set the paths to the possible PowerShell 7 installations
set pwshPath1="C:\Program Files\PowerShell\7\pwsh.exe"
set pwshPath2="D:\Program Files\PowerShell\7\pwsh.exe"

REM Check if pwsh.exe exists in the first path
if exist %pwshPath1% (
    %pwshPath1% -Command "[Console]::OutputEncoding = [System.Text.Encoding]::GetEncoding('shift_jis'); Set-Location '%~dp0'; .\ExportDetailCsv.ps1" > "%~dp0ExportDetailCsv.csv"
    goto end
)

REM Check if pwsh.exe exists in the second path
if exist %pwshPath2% (
    %pwshPath2% -Command "[Console]::OutputEncoding = [System.Text.Encoding]::GetEncoding('shift_jis'); Set-Location '%~dp0'; .\ExportDetailCsv.ps1" > "%~dp0ExportDetailCsv.csv"
    goto end
)

REM If neither path exists, show an error message
echo PowerShell 7 is not installed in "C:\Program Files\PowerShell\7" or "D:\Program Files\PowerShell\7".
echo Please install PowerShell 7 to run this script.
pause

:end