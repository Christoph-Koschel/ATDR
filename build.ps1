function Add-Extension {
    param (
        [string] $project,
        [string] $assembly
    )

    dotnet build $project
    Copy-Item $project/bin/Debug/net4.6.2/$assembly -Destination ./atdr/bin/Debug/net4.6.2/ext
    # Remove-Item ./atdr/bin/Debug/net4.6.2/$assembly
}

Write-Host "Compile ATDR"
dotnet build atdr

Write-Host "Make extension folder"
if (-Not (Test-Path ./atdr/bin/Debug/net4.6.2/ext)) {
    mkdir ./atdr/bin/Debug/net4.6.2/ext
}


Write-Host "Compile extensions"
Add-Extension atdr.basics ATDR.Basics.dll