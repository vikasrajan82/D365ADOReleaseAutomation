[CmdletBinding()]

param()

$ErrorActionPreference = 'Stop'

Write-Host "Entering the AccessTeamTemplate processing"

# Input Parameters
$connectionString = Get-VstsInput -Name connectionstring -Require
$configurationFile = Get-VstsInput -Name configfile -Require

$ScriptDir = Split-Path $MyInvocation.MyCommand.Path

$assembly = [Reflection.Assembly]::LoadFile("$ScriptDir\bin\D365.Xrm.CICD.UpsertRecord.dll")
$assembly = [Reflection.Assembly]::LoadFile("$ScriptDir\bin\Newtonsoft.Json.dll")
$assembly = [Reflection.Assembly]::LoadFile("$ScriptDir\bin\Microsoft.Xrm.Sdk.dll")
$assembly = [Reflection.Assembly]::LoadFile("$ScriptDir\bin\Microsoft.Xrm.Sdk.Deployment.dll")
$assembly = [Reflection.Assembly]::LoadFile("$ScriptDir\bin\Microsoft.Xrm.Tooling.Connector.dll")
$assembly = [Reflection.Assembly]::LoadFile("$ScriptDir\bin\Microsoft.IdentityModel.Clients.ActiveDirectory.dll")
$assembly = [Reflection.Assembly]::LoadFile("$ScriptDir\bin\Microsoft.Crm.Sdk.Proxy.dll")

$instance = New-Object D365.Xrm.CICD.UpsertRecord.D365AccessTeamTemplate -ArgumentList $connectionString
$instance.ProcessTeamTemplates($configurationFile)
