# Tokamak plasma shapes, 1:100

Plasma cross-sections (last closed flux surface, i.e. the separatrix) of 7 tokamaks, plus the WHAM magnetic mirror, at 1:100 scale: 1 m on the real machine = 10 mm on the model. All STEP and STL files are in millimetres. WHAM is described in its own section below.

## Shapes

| Machine | Shape source | R0 (m) | a (m) | κ | δ upper / lower | X-points | Plasma volume | Model size, Ø × height (mm) |
|---|---|---|---|---|---|---|---|---|
| WEST   | digitised: WEST machine capabilities 2024, p. 1 | 2.535 | 0.392 | 1.53 | 0.34 / 0.69 | lower | 11.3 m³ | 58.5 × 12.0 |
| EAST   | digitised: Chen et al. 2019, Fig. 1 | 1.859 | 0.441 | 1.66 | 0.33 / 0.56 | lower | 10.6 m³ | 46.0 × 14.6 |
| DIII-D | digitised: Jackson et al. 2009, Fig. 1(b), t = 0.32 s | 1.755 | 0.573 | 1.82 | 0.39 / 0.64 | lower | 18.1 m³ | 46.6 × 20.9 |
| SPARC  | digitised: Creely et al. 2020, Fig. 2 | 1.847 | 0.564 | 2.01 | 0.54 / 0.54 | upper + lower | 20.1 m³ | 48.2 × 22.7 |
| ITER   | parametric model | 6.200 | 2.000 | 1.85 | 0.49 / 0.49 | lower | 809 m³ | 164 × 74 |
| ARC (V3A) | parametric model | 4.620 | 1.180 | 1.80 | 0.65 / 0.65 | upper + lower (assumed) | 202 m³ | 116 × 42.5 |
| NSTX-U | digitised: TokaMaker equilibrium (Matthew Parsons) | 0.934 | 0.564 | 2.16 | 0.31 / 0.57 | lower | 11.3 m³ | 30.0 × 24.4 |

For the digitised machines, R0, a, κ and δ are measured from the traced shape. They describe that particular equilibrium, not the machine's design limits. The WEST discharge in the capabilities figure is smaller than the nominal a = 0.5 m, and the DIII-D shape is a ramp-up shape.

NSTX-U is a fully shaped, lower-single-null, 1 MA equilibrium solved with TokaMaker within the coil current limits. It is a designed scenario, not a measured shot. Its traced shape agrees with the notebook's own printout:

| | Notebook printout | Traced shape |
|---|---|---|
| κ | 2.153 | 2.16 |
| δ upper / lower | 0.305 / 0.566 | 0.31 / 0.57 |
| Volume | 11.33 m³ | 11.31 m³ |

The previous NSTX-U shape (Pajares et al. 2025, t = 325 ms ramp-up, κ 1.73, 9.2 m³) is kept in `archive_NSTXU_Pajares2025/`.

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
   - **NSTX-U:** 182.5 / 182.25 px per m, from the saved separatrix plot in `NSTXU_shape_generator.ipynb` (cell 31).
2. **Tracing.** Rays are cast outward from inside the plasma every 0.25°. The boundary point is the centre of the first boundary-coloured line each ray meets:
   - **EAST:** black.
   - **DIII-D:** the pink dashed 0.32 s line.
   - **NSTX-U:** the dark-purple separatrix line.
   - **WEST and SPARC:** red.
   
   Rays are blocked beyond the X-points (so divertor legs are ignored) and on annotation arrows. Outliers are rejected against a running median.
3. **X-points.** Each X-point is found by intersecting straight lines fitted to the two traced branches near the corner. The corner angles come out at 71–89°.
4. **Fitting.** The boundary is split at:
   - the outboard and inboard extremes (where the tangent is vertical);
   - each X-point (a sharp corner).
   
   Each piece is fitted with a least-squares cubic B-spline. Its smoothing is raised only until the piece's deepest inward dent is under 0.5 mm at real scale (5 µm on the model), so the outlines are convex to within print resolution.
   - **Fit error:** 1–3.2 mm RMS at real scale (≤ 0.03 mm on the model).
   - **Worst point:** under 14 mm (0.14 mm on the model) for every machine.
   - **NSTX-U X-point:** fixed at TokaMaker's printed value (0.615, −1.322) instead of being refined from the trace.
5. **ITER and ARC** have no reference figure, so they use the parametric model from v2 (Miller form for the rounded half, conic arcs fitted to SPARC for the X-point halves). Those are then fitted the same way.

## Name lettering (`emboss_names_solidworks.ps1` + `Emboss.cs`, run in SolidWorks)
- Arial Black capitals follow the top ring of the torus and read left to right when viewed from above.
- Each letter has a flat top 0.8 mm (4 × 0.2 mm layers) above the highest surface point under it, so the letters finish on a single clean top layer.
- Letters extend straight down into the body and are merged with it, giving one solid body.
- The text sits on the band of the top surface where the surface drops least across the letter height.
- **SPARC and ARC:** these have an X-point ridge on top, so their text sits just outboard of the ridge.
- **NSTX-U:** its crown ring is tiny (R ≈ 7.6 mm at 1:100), so its text is moved just outboard, to R = 10 mm. There the end letters turn about 42° instead of the name wrapping around the hole.

| Machine | Cap height (mm) | Text radius (mm) | Letter-top Z (mm) | Max surface drop under text (mm) |
|---|---|---|---|---|
| WEST   | 4   | 24.5 | 6.66  | 0.92 |
| EAST   | 4   | 17.5 | 7.21  | 1.05 |
| DIII-D | 4   | 15.5 | 10.47 | 0.80 |
| SPARC  | 4   | 16.6 | 12.15 | 2.17 |
| ITER   | 8   | 52.4 | 37.80 | 1.19 |
| ARC    | 6   | 40.7 | 22.03 | 2.45 |
| NSTX-U | 2.8 | 10.0 | 11.82 | 2.08 |

Re-import check of `step_named/`: every file is 1 solid body with 0 geometry faults.

Top-view renders are in `verification/<MACHINE>_named_topview.png`.

**Printing:** print upright, with the machine axis vertical and the letters facing up. The torus has no flat face to sit on; it rests on its lowest ring. So either use supports under the lower half (tree supports work well), or cut it in the slicer, for example at Z = 0, and print the upper half lettering-up. NSTX-U's 2.8 mm letters have strokes about 0.55 mm wide, so a 0.4 mm nozzle and 0.12–0.16 mm layers are recommended for that one.

## WHAM (Wisconsin HTS Axisymmetric Mirror), `build_wham_solidworks.ps1` (`WhamTrace.cs` + `WhamSw.cs`)
**Source.** The pink plasma is digitised from Fig. 2 of Endrizzi et al. 2023 (`../reference_figures/WHAM_fig.png`).
- **Scale:** the tick marks give 428.4 px/m along Z and 428.9 px/m across, so the figure is drawn at equal scale.
- **Folding:** the plasma is axisymmetric and symmetric about Z = 0, so the top and bottom edges of both halves are pooled and median-filtered.

**Tracing.**
- **Central cell:** the plasma is drawn with a soft edge, so the boundary is taken at half the local pink intensity.
- **Mirror throats:** the pink fills the bore, so the boundary there is the vessel wall.
- **Expander fans:** these have hard edges.
- **End walls:** each fan ends at the red hatched end wall, modelled as a flat disc plus a cone. The left wall is drawn 3 cm further out than the right, so the average is used.

Verification overlay: `verification/WHAM_trace_vs_figure.png`.

| | Real | 1:100 |
|---|---|---|
| Plasma radius at the centre | 8.2 cm | 0.82 mm |
| Radius in the mirror throats (\|Z\| = 0.86–1.09 m) | 2.0 cm | 0.20 mm |
| Radius at the expander rim | 22.2 cm | 2.22 mm |
| Total length (end wall to end wall) | 4.14 m | 41.4 mm |
| Plasma volume | 0.133 m³ | 132.6 mm³ |

**Files.**
- **`step/WHAM_plasma_1to100.step` and `stl/WHAM_plasma_1to100.stl`:** the full 1:100 solid of revolution.
  - **Orientation:** the mirror axis is along X and the centre is at the origin.
  - **Check:** 1 solid, 0 faults; the volume matches the analytic π∫r² dz.
  - **Printability:** it is about 1.6 mm thick at the centre and 0.4 mm at the throats. That is too thin to print on its own or to carry lettering.
- **`step_named/WHAM_plaque_named_1to100.step` and `stl_named/...`: the one to print.** It carries the same 1:100 shape:
  - **Relief:** the half-body (cut along the axis) stands up to 2.2 mm proud of a 48 × 15.5 × 2 mm base.
  - **Lettering:** "WHAM" is raised 0.8 mm below the relief, in 5 mm Arial Black.
  - **Check:** 1 solid, 0 faults.
  - **Printing:** print it flat, base down, with no supports.
  - **Renders:** `verification/WHAM_named_topview.png` and `verification/WHAM_named_isoview.png`.
- **`comparison_cross_sections.svg`:** WHAM is drawn at the same scale, lying on its side below the small tokamaks. Run the WHAM script after `Build.Run` to put it back after regenerating.

## STEP validation (SolidWorks import, `check_step_solidworks.ps1`)
| File | Solid bodies | Faces | Check faults | Volume in SolidWorks (mm³) | Analytic (mm³) |
|---|---|---|---|---|---|
| WEST   | 1 | 6 | 0 | 11 267  | 11 264  |
| EAST   | 1 | 6 | 0 | 10 621  | 10 623  |
| DIII-D | 1 | 6 | 0 | 18 058  | 18 057  |
| SPARC  | 1 | 8 | 0 | 20 100  | 20 099  |
| ITER   | 1 | 6 | 0 | 808 588 | 808 590 |
| ARC    | 1 | 8 | 0 | 202 277 | 202 249 |
| NSTX-U | 1 | 6 | 0 | 11 311  | 11 312  |

Analytic volume = 2π × (centroid R) × (cross-section area), at 1:100 (1 m³ real = 1000 mm³).

## Acknowledgements
Many thanks to **Matthew Parsons** for providing the correct NSTX-U plasma shape and the files to generate it:
- `parsons_files/NSTXU_shape_generator.ipynb`: a TokaMaker NSTX-U shape generator and control-point selector.
- `parsons_files/omfit_shape_generator.py`: the OMFIT `boundaryShape` generator.

The NSTX-U model in this set is built from his equilibrium.

## Sources
- EAST: Y.-Y. Chen et al., "Plasma shape optimization for EAST tokamak using orthogonal method", Chin. Phys. B 28 (2019) 015201, Fig. 1. (`EAST_paper.pdf`)
- DIII-D: G.L. Jackson et al., "Simulating ITER plasma startup and rampdown scenarios in the DIII-D tokamak", Nucl. Fusion 49 (2009) 115027, Fig. 1(b), 0.32 s. (`DIIID_paper.pdf`)
- NSTX-U: TokaMaker (OpenFUSIONToolkit) free-boundary LSN equilibrium from Matthew Parsons, `parsons_files/NSTXU_shape_generator.ipynb`:
  - **Inputs:** Ip 1 MA; target shape from the OMFIT `boundaryShape` (Luce 2013) generator.
  - **Figure used:** the saved plot is `../reference_figures/NSTXU_TokaMaker_separatrix.png`.
  - **Not re-run here:** the notebook needs the OFT install and `NSTXU_mesh.h5`.
  - **Previously used:** A. Pajares et al., Fus. Eng. Des. 220 (2025) 115302, t = 325 ms (`NSTXU_paper.pdf`).
- WEST: "WEST machine capabilities for 2024" (CEA), p. 1 figure. (`WEST_paper.pdf`)
- SPARC: A.J. Creely et al., "Overview of the SPARC tokamak", J. Plasma Phys. 86 (2020) 865860502, Fig. 2.
- ITER (separatrix κ 1.85, δ 0.49): ITER-FEAT design parameters, https://fire.pppl.gov/iter_feat_optimize.pdf
- ARC V3A (R0 4.62, a 1.18, κsep 1.8, δsep 0.65): arXiv 2606.09359 (Table 1) and arXiv 2608.10124.
- WHAM: D. Endrizzi et al., "Physics basis for the Wisconsin HTS Axisymmetric Mirror (WHAM)", J. Plasma Phys. 89 (2023) 975890501, Fig. 2.

## Regenerating
From the `Tokamak Comparison` folder, in Windows PowerShell:
```
Add-Type -Path tokamak_models\TokamakGen.cs,tokamak_models\Trace.cs,tokamak_models\Figures.cs,tokamak_models\Build.cs -ReferencedAssemblies System.Drawing
[Build]::Run((Get-Location).Path)
```
`tokamak_models\check_step_solidworks.ps1` opens every STEP file in SolidWorks and reports body count, face count and volume.

`tokamak_models\emboss_names_solidworks.ps1` re-letters every machine; add `-Only NSTX-U` to re-letter one.

`tokamak_models\build_wham_solidworks.ps1` re-traces WHAM, rebuilds its solid and plaque in SolidWorks, and adds it to the SVG.
