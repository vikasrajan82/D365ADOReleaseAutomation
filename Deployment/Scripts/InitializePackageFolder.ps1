# Create Package Directory

$ScriptDir = Split-Path $script:MyInvocation.MyCommand.Path
$PackageDirName = "Packages"
$PackageDir = "$ScriptDir\..\Release\$PackageDirName"

if (Test-Path -Path $PackageDir -PathType Container)
{
   # &cmd.exe /c rd /s /q $PackageDir
   Remove-Item -Recurse -Force $PackageDir   
}

Write-Host $PackageDir

New-Item -Path "$ScriptDir\..\Release\" -Name "$PackageDirName" -Type Directory

if (Test-Path -Path $PackageDir -PathType Container)
{
    Set-Location $PackageDir

    # Initialize NPM (node_modules) and Install Prerequisites
    npm init -y
    npm install azure-pipelines-task-lib --save
    npm install @types/node --save-dev
    npm install @types/q --save-dev
    Save-Module -Name VstsTaskSdk -Path .\

    Set-Location ..
}
