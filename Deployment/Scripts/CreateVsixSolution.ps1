﻿# Create Deployment Directory

$ScriptDir = Split-Path $script:MyInvocation.MyCommand.Path
$DeploymentDirName = "VSIX"
$DeploymentDir = "$ScriptDir\..\$DeploymentDirName"
$ReleaseDir = "$ScriptDir\..\Release"

if (Test-Path -Path $DeploymentDir -PathType Container)
{
   Remove-Item -Recurse -Force $DeploymentDir   
}

New-Item -Path "$ScriptDir\..\" -Name "$DeploymentDirName" -Type Directory

if (Test-Path -Path $DeploymentDir -PathType Container)
{
   Copy-Item "$ReleaseDir\Tasks" $DeploymentDir -Force -Recurse

   Copy-Item "$ReleaseDir\Images\ExtensionIcon.png" $DeploymentDir -Force -Recurse

   Copy-Item "$ReleaseDir\Docs\overview.md" $DeploymentDir -Force -Recurse

   # Updating the VSS Extension file with the created tasks
   $vssExtensionMetadata = Get-Content -Raw -Path "$ReleaseDir\vss-extension.json" | ConvertFrom-Json
   $vssExtensionMetadata.files = @()
   $vssExtensionMetadata.contributions = @()

   $taskFolders = Get-ChildItem "$DeploymentDir\Tasks" -directory

   foreach($taskFolder in $taskFolders)
    {
        # Incrementing the patch in Task.json file
        $taskJson = Get-Content -Raw -Path "$ReleaseDir\Tasks\$taskFolder\task.json" | ConvertFrom-Json
        $taskJson.version.Patch = $taskJson.version.Patch + 1
        $taskJson | ConvertTo-Json -depth 100 | Set-Content "$ReleaseDir\Tasks\$taskFolder\task.json"
        Copy-Item "$ReleaseDir\Tasks\$taskFolder\task.json" "$DeploymentDir\Tasks\$taskFolder" -Force -Recurse

        # Copying the bin folder
         Switch ($taskFolder)
        {
            "D365_ImportSolutionByConfig" 
            { 
                Copy-Item "$ReleaseDir\..\..\Code\D365.Xrm.CICD.ADOExtension\D365.Xrm.CICD.SolutionCustomization\bin\Debug" "$DeploymentDir\Tasks\$taskFolder\bin" -Force -Recurse
                break;
            }
        }

        Copy-Item -Path "$ReleaseDir\Packages\node_modules" -Destination "$DeploymentDir\Tasks\$taskFolder\node_modules" -Force -Recurse

	    Copy-Item -Path "$ReleaseDir\Images\icon.png" -Destination "$DeploymentDir\Tasks\$taskFolder"
	    New-Item "$DeploymentDir\Tasks\$taskFolder\ps_modules\VstsTaskSdk" -ItemType directory
	    Copy-Item -Path "$ReleaseDir\Packages\VstsTaskSdk\0.11.0\*.*" -Destination "$DeploymentDir\Tasks\$taskFolder\ps_modules\VstsTaskSdk"

        Copy-Item -Path "$ReleaseDir\Packages\node_modules" -Destination "$DeploymentDir\Tasks\$taskFolder\node_modules" -Force -Recurse

        $vssExtensionMetadata.files +=  @{ path="Tasks/$taskFolder" }
        $vssExtensionMetadata.contributions += @('{"id": "' + $taskFolder + '", "type": "ms.vss-distributed-task.task", "targets": ["ms.vss-distributed-task.tasks"],"properties": {"name": "Tasks/' + $taskFolder + '"}}' | ConvertFrom-Json)
    }

    # Writing the content to extension file
    $vssExtensionMetadata | ConvertTo-Json -depth 100 | Set-Content "$ReleaseDir\vss-extension.json"
    Copy-Item "$ReleaseDir\vss-extension.json" $DeploymentDir -Force -Recurse

    Set-Location $DeploymentDir

    tfx extension create --manifest-globs vss-extension.json --rev-version

    Set-Location ..

    Copy-Item "$DeploymentDir\vss-extension.json" $ReleaseDir -Force -Recurse
}
