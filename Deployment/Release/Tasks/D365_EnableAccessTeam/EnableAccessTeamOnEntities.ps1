[CmdletBinding()]

param()

$ErrorActionPreference = 'Stop'

Write-Host "Entering the EnableAccessTeam processing"

# Input Parameters
$connectionString = Get-VstsInput -Name connectionstring -Require
$entityList = Get-VstsInput -Name entitylist -Require

$ScriptDir = Split-Path $MyInvocation.MyCommand.Path

$assembly = [Reflection.Assembly]::LoadFile("$ScriptDir\bin\D365.Xrm.CICD.EnableAccessTeam.dll")
$assembly = [Reflection.Assembly]::LoadFile("$ScriptDir\bin\Microsoft.Xrm.Sdk.dll")
$assembly = [Reflection.Assembly]::LoadFile("$ScriptDir\bin\Microsoft.Xrm.Sdk.Deployment.dll")
$assembly = [Reflection.Assembly]::LoadFile("$ScriptDir\bin\Microsoft.Xrm.Tooling.Connector.dll")
$assembly = [Reflection.Assembly]::LoadFile("$ScriptDir\bin\Microsoft.IdentityModel.Clients.ActiveDirectory.dll")
$assembly = [Reflection.Assembly]::LoadFile("$ScriptDir\bin\Microsoft.Crm.Sdk.Proxy.dll")

$instance = New-Object D365.Xrm.CICD.EnableAccessTeam.D365EnableAccess -ArgumentList $connectionString
$instance.EnableAccessTeam($entityList)


    




