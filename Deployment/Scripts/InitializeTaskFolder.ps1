# Create Package Directory

$ScriptDir = Split-Path $script:MyInvocation.MyCommand.Path

Set-Location $ScriptDir\..\Release\Tasks

$TaskName = Read-Host "Enter Task Name: "
if ($TaskName.Length -gt 0)
{
    New-Item -Name $TaskName -Type Directory
    Set-Location $TaskName

    # Only required to have a unqiue GUID
    $GUID = (New-Guid).Guid
    $Name = $TaskName
    $FriendlyName = "Deploy CRM"
    $Description = "Will Deploy CRM solutions to target environment" 
    $Author = "Vikas Rajan"

    $JSON = @"
{
"id": "${GUID}",
"name": "${Name}",
"friendlyName": "${FriendlyName}",
"description": "${Description}",
"helpMarkDown": "",
"category": "Utility",
"author": "${Author}",
"version": {
        "Major": 0,
        "Minor": 1,
        "Patch": 0
    },
    "visibility": [ "Build", "Release" ],
    "instanceNameFormat": "Applying Solution Upgrade: baseurl from AirTable",
    "inputs": [
        {
            "name": "baseurl",
            "type": "string",
            "label": "Base URL",
            "defaultValue": "",
            "required": true,
            "helpMarkDown": "The base URL for the REST API call within AirTable"
        },
        {
            "name": "apikey",
            "type": "string",
            "label": "API Key",
            "inputMode": "passwordbox",
            "isConfidential": true,            
            "defaultValue": "",
            "required": true,
            "helpMarkDown": "AirTable API Key"
        }
    ],
    "execution": {
        "PowerShell3": {
            "target": "print.ps1"
        }
    }
}
"@
    
}