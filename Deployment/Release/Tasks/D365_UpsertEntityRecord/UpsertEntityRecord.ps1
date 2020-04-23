[CmdletBinding()]

param()

$ErrorActionPreference = 'Stop'

Write-Host "Entering the UpsertEntityRecord processing"

# Input Parameters
$connectionString = Get-VstsInput -Name connectionstring -Require
$entityName = Get-VstsInput -Name entityname -Require
$retrieveRecordBy = Get-VstsInput -Name retrieverecord -Require
$recordGuid = Get-VstsInput -Name retrieverecordbyid
$recordFetchXml = Get-VstsInput -Name retrieverecordbyfetchxml
$nameValueJson = Get-VstsInput -Name namevaluepair -Require
$createRecord = Get-VstsInput -Name createrecord -Require -AsBool

$ScriptDir = Split-Path $MyInvocation.MyCommand.Path

$assembly = [Reflection.Assembly]::LoadFile("$ScriptDir\bin\D365.Xrm.CICD.UpsertRecord.dll")
$assembly = [Reflection.Assembly]::LoadFile("$ScriptDir\bin\Newtonsoft.Json.dll")
$assembly = [Reflection.Assembly]::LoadFile("$ScriptDir\bin\Microsoft.Xrm.Sdk.dll")
$assembly = [Reflection.Assembly]::LoadFile("$ScriptDir\bin\Microsoft.Xrm.Sdk.Deployment.dll")
$assembly = [Reflection.Assembly]::LoadFile("$ScriptDir\bin\Microsoft.Xrm.Tooling.Connector.dll")
$assembly = [Reflection.Assembly]::LoadFile("$ScriptDir\bin\Microsoft.IdentityModel.Clients.ActiveDirectory.dll")
$assembly = [Reflection.Assembly]::LoadFile("$ScriptDir\bin\Microsoft.Crm.Sdk.Proxy.dll")

$instance = New-Object D365.Xrm.CICD.UpsertRecord.D365UpsertRecord -ArgumentList $connectionString,$entityName,$nameValueJson,$createRecord

Switch ($retrieveRecordBy)
{
    "recordbyid" 
    { 
        $instance.ProcessRecordByGuid($recordGuid)
        break;
    }
    "recordbyfetchxml" 
    { 
        $instance.ProcessRecordByFetchXml($recordFetchXml)
        break;
    }
}

    




