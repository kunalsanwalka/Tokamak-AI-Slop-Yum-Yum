# Opens each STEP in SolidWorks (via its .NET interop) and reports solid bodies, faces, geometry-check faults and volume.
param([string]$StepDir = "$PSScriptRoot\step")
$interop = "C:\Program Files\SOLIDWORKS Corp\SOLIDWORKS\api\redist\SolidWorks.Interop.sldworks.dll"
[void][Reflection.Assembly]::LoadFrom($interop)
Add-Type -Path "$PSScriptRoot\SwCheck.cs" -ReferencedAssemblies $interop
[SwCheck]::Run((Resolve-Path $StepDir).Path)
