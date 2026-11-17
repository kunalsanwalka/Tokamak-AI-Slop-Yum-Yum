# Tokamak plasma shapes, 1:100

Plasma cross-sections (last closed flux surface, i.e. the separatrix) of 7 tokamaks at 1:100 scale: 1 m on the real machine = 10 mm on the model. All STEP and STL files are in millimetres.

## Shapes

| Machine | Shape source | R0 (m) | a (m) | κ | δ upper / lower | X-points | Plasma volume | Model size, Ø × height (mm) |
|---|---|---|---|---|---|---|---|---|
| WEST   | digitised: WEST machine capabilities 2024, p. 1 | 2.535 | 0.392 | 1.53 | 0.34 / 0.69 | lower | 11.3 m³ | 58.5 × 12.0 |
| EAST   | digitised: Chen et al. 2019, Fig. 1 | 1.859 | 0.441 | 1.66 | 0.33 / 0.56 | lower | 10.6 m³ | 46.0 × 14.6 |
| DIII-D | digitised: Jackson et al. 2009, Fig. 1(b), t = 0.32 s | 1.755 | 0.573 | 1.82 | 0.39 / 0.64 | lower | 18.1 m³ | 46.6 × 20.9 |
| SPARC  | digitised: Creely et al. 2020, Fig. 2 | 1.847 | 0.564 | 2.01 | 0.54 / 0.54 | upper + lower | 20.1 m³ | 48.2 × 22.7 |
| ITER   | parametric model | 6.200 | 2.000 | 1.85 | 0.49 / 0.49 | lower | 809 m³ | 164 × 74 |
| ARC (V3A) | parametric model | 4.620 | 1.180 | 1.80 | 0.65 / 0.65 | upper + lower (assumed) | 202 m³ | 116 × 42.5 |
| NSTX-U | digitised: Pajares et al. 2025, t = 325 ms | 0.914 | 0.583 | 1.73 | 0.39 / 0.51 | lower | 9.2 m³ | 29.9 × 20.5 |

For the digitised machines, R0, a, κ and δ are measured from the traced shape. They describe that particular equilibrium, not the machine's design limits. The WEST discharge in the capabilities figure is smaller than the nominal a = 0.5 m, and the DIII-D and NSTX-U shapes are ramp-up shapes.

## Files
- `step_named/<MACHINE>_plasma_named_1to100.step` and `stl_named/<MACHINE>_plasma_named_1to100.stl`: the full solid with the device name raised on the top. **These are the ones to print.** Details are in the "Name lettering" section below.
- `step/<MACHINE>_plasma_1to100.step`: the full 360° solid of revolution about the machine axis (Z axis), with the origin on the axis at Z = 0. The boundary curves are exact cubic B-splines, revolved as true surfaces of revolution, with sharp X-point corners. These are the CAD masters.
- `stl/<MACHINE>_cutaway270_1to100.stl`: 3/4 torus with a 90° wedge removed so the cross-section shows. It stands on its lowest point (needs supports).
- `stl/<MACHINE>_profile_plate_1to100.stl`: the cross-section extruded 3 mm thick. It prints flat with no supports. x = R, so the plates sit at their true radius from a shared axis.
- `csv/`: 600–800 boundary points per machine, R and Z in metres and in 1:100 mm.
- `comparison_cross_sections.svg`: all 7 shapes at true scale on common R and Z axes (same scale on both axes).
- `verification/<MACHINE>_trace_vs_figure.png`: each source figure with the traced points (green), the fitted boundary (thin black) and the X-points (blue circles) drawn on top.
- `../reference_figures/`: the figures used for digitising.

## How the shapes were made
1. **Calibration.** Each figure's pixel-to-metre scale comes from its axis tick marks or tick-label centres:
   - **EAST:** 183.7 / 184.0 px per m (R / Z).
   - **WEST:** 326.5 / 326.75 px per m.
   - **SPARC:** 145.4 / 145.4 px per m.
   - **DIII-D:** 267 / 271 px per m. The Z ticks in that figure are unevenly spaced (±7 px), so a least-squares fit is used; the remaining error is about ±2.5 cm at real scale (±0.25 mm on the model).
   - **NSTX-U:** the panel has no Z axis, so equal aspect is assumed, with Z = 0 at the vertical centre of the machine drawing.
2. **Tracing.** Rays are cast outward from inside the plasma every 0.25°. The boundary point is the centre of the first boundary-coloured line each ray meets:
   - **EAST:** black.
   - **DIII-D:** the pink dashed 0.32 s line.
   - **NSTX-U:** the thick cyan line.
   - **WEST and SPARC:** red.
   
   Rays are blocked beyond the X-points (so divertor legs are ignored) and on annotation arrows. Outliers are rejected against a running median.
3. **X-points.** Each X-point is found by intersecting straight lines fitted to the two traced branches near the corner. The corner angles come out at 71–89°.
4. **Fitting.** The boundary is split at:
   - the outboard and inboard extremes (where the tangent is vertical);
   - each X-point (a sharp corner).
   
   Each piece is fitted with a least-squares cubic B-spline. Its smoothing is raised only until the piece's deepest inward dent is under 0.5 mm at real scale (5 µm on the model), so the outlines are convex to within print resolution.
   - **Fit error:** 1–3 mm RMS at real scale (≤ 0.03 mm on the model), or about 6 mm for the low-resolution NSTX-U image.
   - **Worst point:** under 13 mm (0.13 mm on the model) for every machine except NSTX-U, where it is 29 mm.
5. **ITER and ARC** have no reference figure, so they use the parametric model from v2 (Miller form for the rounded half, conic arcs fitted to SPARC for the X-point halves). Those are then fitted the same way.

## Name lettering (`emboss_names_solidworks.ps1` + `Emboss.cs`, run in SolidWorks)
- Arial Black capitals follow the top ring of the torus and read left to right when viewed from above.
- Each letter has a flat top 0.8 mm (4 × 0.2 mm layers) above the highest surface point under it, so the letters finish on a single clean top layer.
- Letters extend straight down into the body and are merged with it, giving one solid body.
- The text sits on the band of the top surface where the surface drops least across the letter height.
- **SPARC and ARC:** these have an X-point ridge on top, so their text sits just outboard of the ridge.
- **NSTX-U:** its crown ring is tiny (R ≈ 7 mm at 1:100), so its text is moved out to R = 11 mm. There the name spans about 100° of arc instead of wrapping around the hole.

| Machine | Cap height (mm) | Text radius (mm) | Letter-top Z (mm) | Max surface drop under text (mm) |
|---|---|---|---|---|
| WEST   | 4   | 24.5 | 6.66  | 0.92 |
| EAST   | 4   | 17.5 | 7.21  | 1.05 |
| DIII-D | 4   | 15.5 | 10.47 | 0.80 |
| SPARC  | 4   | 16.6 | 12.15 | 2.17 |
| ITER   | 8   | 52.4 | 37.80 | 1.19 |
| ARC    | 6   | 40.7 | 22.03 | 2.45 |
| NSTX-U | 2.8 | 11.0 | 10.26 | 2.53 |

Re-import check of `step_named/`: every file is 1 solid body with 0 geometry faults.

Top-view renders are in `verification/<MACHINE>_named_topview.png`.

**Printing:** print upright, with the machine axis vertical and the letters facing up. The torus has no flat face to sit on; it rests on its lowest ring. So either use supports under the lower half (tree supports work well), or cut it in the slicer, for example at Z = 0, and print the upper half lettering-up. NSTX-U's 2.8 mm letters have strokes about 0.55 mm wide, so a 0.4 mm nozzle and 0.12–0.16 mm layers are recommended for that one.

## STEP validation (SolidWorks import, `check_step_solidworks.ps1`)
| File | Solid bodies | Faces | Check faults | Volume in SolidWorks (mm³) | Analytic (mm³) |
|---|---|---|---|---|---|
| WEST   | 1 | 6 | 0 | 11 267  | 11 264  |
| EAST   | 1 | 6 | 0 | 10 621  | 10 623  |
| DIII-D | 1 | 6 | 0 | 18 058  | 18 057  |
| SPARC  | 1 | 8 | 0 | 20 100  | 20 099  |
| ITER   | 1 | 6 | 0 | 808 588 | 808 590 |
| ARC    | 1 | 8 | 0 | 202 277 | 202 249 |
| NSTX-U | 1 | 6 | 0 | 9 198   | 9 197   |

Analytic volume = 2π × (centroid R) × (cross-section area), at 1:100 (1 m³ real = 1000 mm³).

## Sources
- EAST: Y.-Y. Chen et al., "Plasma shape optimization for EAST tokamak using orthogonal method", Chin. Phys. B 28 (2019) 015201, Fig. 1. (`EAST_paper.pdf`)
- DIII-D: G.L. Jackson et al., "Simulating ITER plasma startup and rampdown scenarios in the DIII-D tokamak", Nucl. Fusion 49 (2009) 115027, Fig. 1(b), 0.32 s. (`DIIID_paper.pdf`)
- NSTX-U: A. Pajares et al., "Plasma shape and position control development for NSTX-U using the GSEvolve plasma simulator", Fus. Eng. Des. 220 (2025) 115302, panel (i), t = 325 ms. (`NSTXU_paper.pdf`)
- WEST: "WEST machine capabilities for 2024" (CEA), p. 1 figure. (`WEST_paper.pdf`)
- SPARC: A.J. Creely et al., "Overview of the SPARC tokamak", J. Plasma Phys. 86 (2020) 865860502, Fig. 2.
- ITER (separatrix κ 1.85, δ 0.49): ITER-FEAT design parameters, https://fire.pppl.gov/iter_feat_optimize.pdf
- ARC V3A (R0 4.62, a 1.18, κsep 1.8, δsep 0.65): arXiv 2606.09359 (Table 1) and arXiv 2608.10124.

## Regenerating
From the `Tokamak Comparison` folder, in Windows PowerShell:
```
Add-Type -Path tokamak_models\TokamakGen.cs,tokamak_models\Trace.cs,tokamak_models\Figures.cs,tokamak_models\Build.cs -ReferencedAssemblies System.Drawing
[Build]::Run((Get-Location).Path)
```
`tokamak_models\check_step_solidworks.ps1` opens every STEP file in SolidWorks and reports body count, face count and volume.
