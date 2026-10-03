@echo off
setlocal enabledelayedexpansion

echo ========================================================
echo   Enterprise Log Redactor - Build Automation
echo ========================================================

set CSC_EXE=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe

if not exist "%CSC_EXE%" (
    set CSC_EXE=C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe
)

if exist "%CSC_EXE%" (
    echo [*] Found Windows Native C# Compiler: %CSC_EXE%
    echo [*] Compiling standalone LogRedactor.exe from src subfolders...
    
    "%CSC_EXE%" /target:winexe /platform:anycpu /optimize+ /out:LogRedactor.exe /r:System.dll,System.Core.dll,System.Drawing.dll,System.Windows.Forms.dll src\Program.cs src\UI\MainForm.cs src\Engine\RedactionEngine.cs src\IO\StreamingLogProcessor.cs src\Models\Models.cs
    
    if !ERRORLEVEL! equ 0 (
        echo.
        echo [OK] Successfully built standalone LogRedactor.exe
        echo [OK] Size:
        for %%I in (LogRedactor.exe) do echo     %%~zI bytes
        echo [OK] Target: Native Windows .NET executable (No runtime installations required)
        goto DONE
    )
)

where dotnet >nul 2>&1
if !ERRORLEVEL! equ 0 (
    echo [*] Found dotnet CLI. Publishing single-file release...
    dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ./publish
    if !ERRORLEVEL! equ 0 (
        echo.
        echo [OK] Successfully published single-file LogRedactor.exe to ./publish/LogRedactor.exe
        goto DONE
    )
)

echo [ERROR] No suitable compiler found. Please ensure .NET Framework or .NET SDK is installed.
pause
exit /b 1

:DONE
echo.
echo Build complete. You can run LogRedactor.exe directly on any Windows machine.
echo ========================================================
