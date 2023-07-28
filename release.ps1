function Add-Extension {
    param (
        [string] $project,
        [string] $assembly
    )

    dotnet publish $project
    Copy-Item $project/bin/Debug/net4.6.2/publish/$assembly -Destination ./atdr/bin/Debug/net4.6.2/publish/ext
}

Write-Host "Compile ATDR"
dotnet publish atdr

Write-Host "Make extension folder"
if (-Not (Test-Path ./atdr/bin/Debug/net4.6.2/publish/ext)) {
    mkdir ./atdr/bin/Debug/net4.6.2/publish/ext
}


Write-Host "Compile extensions"
Add-Extension atdr.basics ATDR.Basics.dll