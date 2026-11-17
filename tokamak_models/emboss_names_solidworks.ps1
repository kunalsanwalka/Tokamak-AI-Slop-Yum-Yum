# Adds the device name on top of each plasma STEP using SolidWorks, exports named STEP/STL and top-view PNGs.
param([switch]$Visible)
$redist = "C:\Program Files\SOLIDWORKS Corp\SOLIDWORKS\api\redist"
[void][Reflection.Assembly]::LoadFrom("$redist\SolidWorks.Interop.sldworks.dll")
[void][Reflection.Assembly]::LoadFrom("$redist\SolidWorks.Interop.swconst.dll")
Add-Type -Path "$PSScriptRoot\Emboss.cs" -ReferencedAssemblies "$redist\SolidWorks.Interop.sldworks.dll","$redist\SolidWorks.Interop.swconst.dll","System.Drawing"
[Emboss]::Run((Resolve-Path "$PSScriptRoot\..").Path, [bool]$Visible)
