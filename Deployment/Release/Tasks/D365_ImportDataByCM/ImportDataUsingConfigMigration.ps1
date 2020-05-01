[CmdletBinding()]

param()

function Write-Trace {
    param(
        [Parameter(Mandatory=$true)]
        [string]$message
    )

    if($enableTracing)
    {
        Write-Host $message
    }
}

$ErrorActionPreference = 'Stop'

Write-Host "Entering the ImportDataUsingConfigurationMigration processing"

# Input Parameters
$connectionString = Get-VstsInput -Name connectionstring -Require
$configFile = Get-VstsInput -Name configfile -Require
$enableTracing = Get-VstsInput -Name enabletracing -Require -AsBool
$replaceGuids = Get-VstsInput -Name replaceguids

If (Test-Path -Path $configFile -PathType Leaf)
{
    Write-Host "$configFile is found. Processing the data import"

    $enableTracing = $True

    Write-Host "Installing Configuration Migration Module........." –NoNewline

    Install-Module -Name Microsoft.Xrm.Tooling.ConfigurationMigration -RequiredVersion 1.0.0.26 -Force -AllowClobber

    Write-Host "Completed"

    $ScriptDir = Split-Path $MyInvocation.MyCommand.Path

    $assembly = [Reflection.Assembly]::LoadFile("$ScriptDir\bin\D365.Xrm.CICD.DataImport.dll")
    $assembly = [Reflection.Assembly]::LoadFile("$ScriptDir\bin\Newtonsoft.Json.dll")
    $assembly = [Reflection.Assembly]::LoadFile("$ScriptDir\bin\Microsoft.Xrm.Sdk.dll")
    $assembly = [Reflection.Assembly]::LoadFile("$ScriptDir\bin\Microsoft.Xrm.Sdk.Deployment.dll")
    $assembly = [Reflection.Assembly]::LoadFile("$ScriptDir\bin\Microsoft.Xrm.Tooling.Connector.dll")
    $assembly = [Reflection.Assembly]::LoadFile("$ScriptDir\bin\Microsoft.IdentityModel.Clients.ActiveDirectory.dll")
    $assembly = [Reflection.Assembly]::LoadFile("$ScriptDir\bin\Microsoft.Crm.Sdk.Proxy.dll")
    
    $instance = New-Object D365.Xrm.CICD.DataImport.D365DataImport -ArgumentList $connectionString,$enableTracing
    $instance.ProcessDataImport($configFile,$replaceGuids)

    #[XML]$xml = Get-Content $configFile

    #$parentfolder = Split-Path -Path $configFile -Parent

    #Write-Trace "Parent Folder: $parentfolder"
    
    #foreach($importfiles in $xml.deploymentartifact.filestoimport.configimportfile)
    #{
    #   $zipfilename = $importfiles.filename
    #    Write-Trace "Zip File Name: $zipfilename"

    #    $zipfilepath = "$parentfolder\MasterData\$zipfilename.zip"

    #    if(Test-Path -Path "$zipfilepath" -PathType Leaf)
    #    {
    #        Write-Host "Importing data for $zipfilename"

    #        Import-CrmDataFile -CrmConnection $connectionString -DataFile "$zipfilepath" -EmitLogToConsole -Verbose -EnabledBatchMode -BatchSize 500

    #        Write-Host "Data Import complete for $zipfilename"
    #    }
    #    else
    #    {
    #        Write-Error "$zipfilepath is not found to import"
    #    }
    #}
}
else
{
    Write-Error "Configuration File ($configFile) is not found. Please check the path again"
}



