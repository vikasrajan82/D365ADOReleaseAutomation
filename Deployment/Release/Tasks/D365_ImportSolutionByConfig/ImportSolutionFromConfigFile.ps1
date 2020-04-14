[CmdletBinding()]

param()

$ErrorActionPreference = 'Stop'

Write-Host "Entering the ImportSolutionFromConfigFile processing"

# Input Parameters
$connectionString = Get-VstsInput -Name connectionstring -Require
$configFile = Get-VstsInput -Name configfile -Require

If (Test-Path -Path $configFile -PathType Leaf)
{

    Write-Host $configFile

    #Write-Verbose "$configfile"

    $ScriptDir = Split-Path $MyInvocation.MyCommand.Path

    Write-Host $ScriptDir

}
else
{
    Write-Error "Configuration File ($configFile is not found. Please check the path again"
}
