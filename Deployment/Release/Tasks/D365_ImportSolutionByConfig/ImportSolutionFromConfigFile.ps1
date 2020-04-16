[CmdletBinding()]

param()

$ErrorActionPreference = 'Stop'

Write-Host "Entering the ImportSolutionFromConfigFile processing"

# Input Parameters
$connectionString = Get-VstsInput -Name connectionstring -Require
$configFile = Get-VstsInput -Name configfile -Require
$enableTracing = Get-VstsInput -Name enabletracing -Require -AsBool

If (Test-Path -Path $configFile -PathType Leaf)
{

    Write-Host "$configFile is found. Processing the solution import"

    $ScriptDir = Split-Path $MyInvocation.MyCommand.Path

    $assembly = [Reflection.Assembly]::LoadFile("$ScriptDir\bin\Microsoft.Crm.Sdk.Proxy.dll")
    $assembly = [Reflection.Assembly]::LoadFile("$ScriptDir\bin\Microsoft.IdentityModel.Clients.ActiveDirectory.dll")
    $assembly = [Reflection.Assembly]::LoadFile("$ScriptDir\bin\Microsoft.Xrm.Sdk.dll")
    $assembly = [Reflection.Assembly]::LoadFile("$ScriptDir\bin\Microsoft.Xrm.Tooling.Connector.dll")
    $assembly = [Reflection.Assembly]::LoadFile("$ScriptDir\bin\Microsoft.Xrm.Sdk.Deployment.dll")
    $assembly = [Reflection.Assembly]::LoadFile("$ScriptDir\bin\D365.Xrm.CICD.SolutionCustomization.dll")
    
    $instance = New-Object D365.Xrm.CICD.SolutionCustomization.D365SolutionImport -ArgumentList $connectionString,$enableTracing
    $instance.ProcessSolutionImport($configFile)
}
else
{
    Write-Error "Configuration File ($configFile) is not found. Please check the path again"
}
