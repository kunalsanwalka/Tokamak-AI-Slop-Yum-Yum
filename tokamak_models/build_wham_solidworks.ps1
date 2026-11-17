# Traces WHAM from reference_figures/WHAM_fig.png and builds the 1:100 solid and the named plaque in SolidWorks.
param([switch]$Visible)
$redist = "C:\Program Files\SOLIDWORKS Corp\SOLIDWORKS\api\redist"
[void][Reflection.Assembly]::LoadFrom("$redist\SolidWorks.Interop.sldworks.dll")
[void][Reflection.Assembly]::LoadFrom("$redist\SolidWorks.Interop.swconst.dll")
Add-Type -Path "$PSScriptRoot\WhamTrace.cs","$PSScriptRoot\WhamSw.cs","$PSScriptRoot\Emboss.cs" -ReferencedAssemblies "$redist\SolidWorks.Interop.sldworks.dll","$redist\SolidWorks.Interop.swconst.dll","System.Drawing"
[WhamSw]::Run((Resolve-Path "$PSScriptRoot\..").Path, [bool]$Visible)
