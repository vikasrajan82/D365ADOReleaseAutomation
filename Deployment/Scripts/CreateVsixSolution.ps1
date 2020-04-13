# Create Deployment Directory

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

   $vssExtensionMetadata = Get-Content -Raw -Path "$ReleaseDir\vss-extension.json" | ConvertFrom-Json
   $vssExtensionMetadata.files = @()
   $vssExtensionMetadata.contributions = @()

   $taskFolders = Get-ChildItem "$DeploymentDir\Tasks" -directory

   foreach($taskFolder in $taskFolders)
    {
	    Copy-Item -Path "$ReleaseDir\Images\ExtensionTaskIcon.png" -Destination "$DeploymentDir\Tasks\$taskFolder"
	    New-Item "$DeploymentDir\Tasks\$taskFolder\ps_modules\VstsTaskSdk" -ItemType directory
	    Copy-Item -Path "$ReleaseDir\Packages\VstsTaskSdk\0.11.0\*.*" -Destination "$DeploymentDir\Tasks\$taskFolder\ps_modules\VstsTaskSdk"

        Copy-Item -Path "$ReleaseDir\Packages\node_modules" -Destination "$DeploymentDir\Tasks\$taskFolder\node_modules" -Force -Recurse

        $vssExtensionMetadata.files +=  @{ path="Tasks/$taskFolder" }
        $vssExtensionMetadata.contributions += @('{"id": "' + $taskFolder + '", "type": "ms.vss-distributed-task.task", "targets": ["ms.vss-distributed-task.tasks"],"properties": {"name": "Tasks/' + $taskFolder + '"}}' | ConvertFrom-Json)
    }

    $vssExtensionMetadata | ConvertTo-Json -depth 100 | Set-Content "$ReleaseDir\vss-extension.json"
    Copy-Item "$ReleaseDir\vss-extension.json" $DeploymentDir -Force -Recurse

    Set-Location $DeploymentDir

    tfx extension create --manifest-globs vss-extension.json --rev-version

    Set-Location ..

    Copy-Item "$DeploymentDir\vss-extension.json" $ReleaseDir -Force -Recurse
}
