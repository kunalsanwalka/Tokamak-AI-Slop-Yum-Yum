# Builds the named 1:100 plaques for 2XIIB, GAMMA 10, GDT and TMX-U from Elliot Claveau's flux-surface STEP files
# (mirror_models/step_raw/) in SolidWorks, thickening anything under 0.8 mm, then redraws mirror_models/comparison_mirrors.svg.
# Use -Only <tag> (2XIIB, GAMMA10, GDT, TMX-U) to rebuild one machine.
param([switch]$Visible, [string]$Only = "")
$redist = "C:\Program Files\SOLIDWORKS Corp\SOLIDWORKS\api\redist"
[void][Reflection.Assembly]::LoadFrom("$redist\SolidWorks.Interop.sldworks.dll")
[void][Reflection.Assembly]::LoadFrom("$redist\SolidWorks.Interop.swconst.dll")
Add-Type -Path "$PSScriptRoot\MirrorSw.cs","$PSScriptRoot\MirrorThin.cs","$PSScriptRoot\MirrorSvg.cs","$PSScriptRoot\WhamTrace.cs","$PSScriptRoot\Emboss.cs" -ReferencedAssemblies "$redist\SolidWorks.Interop.sldworks.dll","$redist\SolidWorks.Interop.swconst.dll","System.Drawing"
$root = (Resolve-Path "$PSScriptRoot\..").Path
[MirrorSw]::Run($root, [bool]$Visible, $Only)
[MirrorSvg]::Run($root)
