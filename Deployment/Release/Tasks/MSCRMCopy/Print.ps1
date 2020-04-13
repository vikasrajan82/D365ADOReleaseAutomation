Write-Host "Checking123"

Write-Host "$env:SYSTEM_DEFAULTWORKINGDIRECTORY"

$ScriptDir = Split-Path $MyInvocation.MyCommand.Path

#Write-Host $MyInvocation.MyCommand.Path

#Get-Location

#Write-Host "After printing directory path"

#Get-ChildItem -Recurse -Directory


$assembly = [Reflection.Assembly]::LoadFile("$ScriptDir\ConsoleLibrary.dll")
$instance = New-Object ConsoleLibrary.Class1
$instance.Print()