[CmdletBinding()]

param()

$ErrorActionPreference = 'Stop'

Write-Host "Entering the ExportDocumentTemplates processing"

# Input Parameters
$connectionString = Get-VstsInput -Name connectionstring -Require
$retrieveRecord = Get-VstsInput -Name retrieverecord -Require
$templateNames = Get-VstsInput -Name templatesbynames
$destinationFolder = Get-VstsInput -Name destinationfolder -Require

$ScriptDir = Split-Path $MyInvocation.MyCommand.Path

$assembly = [Reflection.Assembly]::LoadFile("$ScriptDir\bin\D365.Xrm.CICD.RetrieveRecord.dll")
$assembly = [Reflection.Assembly]::LoadFile("$ScriptDir\bin\Newtonsoft.Json.dll")
$assembly = [Reflection.Assembly]::LoadFile("$ScriptDir\bin\Microsoft.Xrm.Sdk.dll")
$assembly = [Reflection.Assembly]::LoadFile("$ScriptDir\bin\Microsoft.Xrm.Sdk.Deployment.dll")
$assembly = [Reflection.Assembly]::LoadFile("$ScriptDir\bin\Microsoft.Xrm.Tooling.Connector.dll")
$assembly = [Reflection.Assembly]::LoadFile("$ScriptDir\bin\Microsoft.IdentityModel.Clients.ActiveDirectory.dll")
$assembly = [Reflection.Assembly]::LoadFile("$ScriptDir\bin\Microsoft.Crm.Sdk.Proxy.dll")

$instance = New-Object D365.Xrm.CICD.RetrieveRecord.D365RetrieveDocumentTemplates -ArgumentList $connectionString,$destinationFolder

Switch ($retrieveRecord)
{
    "alltemplates" 
    { 
        $instance.GenerateAllTemplates()
        break;
    }
    "templatesbynames" 
    { 
        $instance.GenerateTemplates($templateNames)
        break;
    }
}

    




