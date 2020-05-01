[CmdletBinding()]

param()

$ErrorActionPreference = 'Stop'

Write-Host "Entering the UpdatePluginConfiguration processing"

# Input Parameters
$connectionString = Get-VstsInput -Name connectionstring -Require
$configurationType = Get-VstsInput -Name configurationtype -Require
$pluginConfiguration = Get-VstsInput -Name pluginconfiguration -Require

$ScriptDir = Split-Path $MyInvocation.MyCommand.Path

$assembly = [Reflection.Assembly]::LoadFile("$ScriptDir\bin\D365.Xrm.CICD.PluginConfiguration.dll")
$assembly = [Reflection.Assembly]::LoadFile("$ScriptDir\bin\Newtonsoft.Json.dll")
$assembly = [Reflection.Assembly]::LoadFile("$ScriptDir\bin\Microsoft.Xrm.Sdk.dll")
$assembly = [Reflection.Assembly]::LoadFile("$ScriptDir\bin\Microsoft.Xrm.Sdk.Deployment.dll")
$assembly = [Reflection.Assembly]::LoadFile("$ScriptDir\bin\Microsoft.Xrm.Tooling.Connector.dll")
$assembly = [Reflection.Assembly]::LoadFile("$ScriptDir\bin\Microsoft.IdentityModel.Clients.ActiveDirectory.dll")
$assembly = [Reflection.Assembly]::LoadFile("$ScriptDir\bin\Microsoft.Crm.Sdk.Proxy.dll")

$instance = New-Object D365.Xrm.CICD.PluginConfiguration.D365UpdatePluginConfiguration -ArgumentList $connectionString

Switch ($configurationType)
{
    "secureconfig" 
    { 
        $instance.ProcessPluginSecureConfigUpdate($pluginConfiguration)
        break;
    }
    "unsecureconfig" 
    { 
        $instance.ProcessPluginUnsecureConfigUpdate($pluginConfiguration)
        break;
    }
}

    




