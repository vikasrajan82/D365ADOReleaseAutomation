[CmdletBinding()]

param()

$ErrorActionPreference = 'Stop'

Write-Host "Entering the RetrieveAccessTeamTemplate processing"

# Input Parameters
$connectionString = Get-VstsInput -Name connectionstring -Require
$destinationFile = Get-VstsInput -Name destinationfile -Require

$ScriptDir = Split-Path $MyInvocation.MyCommand.Path

$assembly = [Reflection.Assembly]::LoadFile("$ScriptDir\bin\D365.Xrm.CICD.RetrieveRecord.dll")
$assembly = [Reflection.Assembly]::LoadFile("$ScriptDir\bin\Newtonsoft.Json.dll")
$assembly = [Reflection.Assembly]::LoadFile("$ScriptDir\bin\Microsoft.Xrm.Sdk.dll")
$assembly = [Reflection.Assembly]::LoadFile("$ScriptDir\bin\Microsoft.Xrm.Sdk.Deployment.dll")
$assembly = [Reflection.Assembly]::LoadFile("$ScriptDir\bin\Microsoft.Xrm.Tooling.Connector.dll")
$assembly = [Reflection.Assembly]::LoadFile("$ScriptDir\bin\Microsoft.IdentityModel.Clients.ActiveDirectory.dll")
$assembly = [Reflection.Assembly]::LoadFile("$ScriptDir\bin\Microsoft.Crm.Sdk.Proxy.dll")

$instance = New-Object D365.Xrm.CICD.RetrieveRecord.D365RetrieveAccessTeams -ArgumentList $connectionString
$instance.GenerateAccessTeamTemplatesJson($destinationFile)
