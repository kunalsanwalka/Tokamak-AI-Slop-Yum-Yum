# Tokamak plasma shapes, 1:100

Plasma cross-sections (last closed flux surface, i.e. the separatrix) of 9 tokamaks, plus five magnetic mirrors (WHAM, 2XIIB, GAMMA 10, GDT, TMX-U), at 1:100 scale: 1 m on the real machine = 10 mm on the model. All STEP and STL files are in millimetres. The tokamak models are in `tokamak_models/`; all the mirror models, and their own comparison drawing `comparison_mirrors.svg`, are in `mirror_models/` and are described in their own sections below.

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
| JET    | digitised: de Vries et al. 2009, Fig. 1 (left) | 2.919 | 0.915 | 1.64 | 0.07 / 0.33 | lower | 73.4 m³ | 76.7 × 30.0 |
| JT-60U | digitised: de Vries et al. 2009, Fig. 1 (right) | 3.383 | 0.850 | 1.55 | 0.11 / 0.30 | lower | 68.7 m³ | 84.7 × 26.3 |

For the digitised machines, R0, a, κ and δ are measured from the traced shape. They describe that particular equilibrium, not the machine's design limits. The WEST discharge in the capabilities figure is smaller than the nominal a = 0.5 m, and the DIII-D shape is a ramp-up shape. The JET and JT-60U shapes are the matched, low-triangularity shapes from the 2008 JT-60U / JET identity experiments on internal transport barriers. In those experiments JET's TF ripple was set to match JT-60U's.

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
- `comparison_cross_sections.svg`: all 9 tokamak shapes (and WHAM) at true scale on common R and Z axes (same scale on both axes).
- `verification/<MACHINE>_trace_vs_figure.png`: each source figure with the traced points (green), the fitted boundary (thin black) and the X-points (blue circles) drawn on top.
- `../reference_figures/`: the figures used for digitising.

## How the shapes were made
1. **Calibration.** Each figure's pixel-to-metre scale comes from its axis tick marks or tick-label centres:
   - **EAST:** 183.7 / 184.0 px per m (R / Z).
   - **WEST:** 326.5 / 326.75 px per m.
   - **SPARC:** 145.4 / 145.4 px per m.
   - **DIII-D:** 267 / 271 px per m. The Z ticks in that figure are unevenly spaced (±7 px), so a least-squares fit is used; the remaining error is about ±2.5 cm at real scale (±0.25 mm on the model).
   - **NSTX-U:** 182.5 / 182.25 px per m, from the saved separatrix plot in `NSTXU_shape_generator.ipynb` (cell 31).
   - **JET and JT-60U:** 163.45 (JET) and 163.15 (JT-60U) px per m in R, and 154.25 px per m in Z for both. That figure is not drawn at equal scale (R is stretched by 6%), so R and Z are calibrated separately.
2. **Tracing.** Rays are cast outward from inside the plasma every 0.25°. The boundary point is the centre of the first boundary-coloured line each ray meets:
   - **EAST:** black.
   - **DIII-D:** the pink dashed 0.32 s line.
   - **NSTX-U:** the dark-purple separatrix line.
   - **WEST and SPARC:** red.
   - **JET and JT-60U:** these panels draw nested black flux surfaces, plus an open line just outside the separatrix.
     - **Which line:** the separatrix is the next line out after the closed surfaces (7 in JET, 9 in JT-60U). On each ray the tracer takes the line closest to the running median of those counts, so a surface hidden under a brown ripple contour or label doesn't shift the count.
     - **Boundary point:** the inner edge of that line plus half its width.
     - **Brown overlays:** brown dashes crossing a line are bridged.
   
   Rays are blocked beyond the X-points (so divertor legs are ignored) and on annotation arrows. Outliers are rejected against a running median.
3. **X-points.** Each X-point is found by intersecting straight lines fitted to the two traced branches near the corner. The corner angles come out at 71–89°.
4. **Fitting.** The boundary is split at:
   - the outboard and inboard extremes (where the tangent is vertical);
   - each X-point (a sharp corner).
   
   Each piece is fitted with a least-squares cubic B-spline. Its smoothing is raised only until the piece's deepest inward dent is under 0.5 mm at real scale (5 µm on the model), so the outlines are convex to within print resolution.
   - **Fit error:** 1–3.2 mm RMS at real scale (≤ 0.03 mm on the model).
   - **Worst point:** under 14 mm (0.14 mm on the model) for every machine.
   - **JET and JT-60U gaps:** near each X-point the line is hidden for a short stretch, by a ripple label in JET, or the rays don't reach it. There the fit runs straight to the X-point, matching the straight separatrix in the figure.
   - **NSTX-U X-point:** fixed at TokaMaker's printed value (0.615, −1.322) instead of being refined from the trace.
5. **ITER and ARC** have no reference figure, so they use the parametric model from v2 (Miller form for the rounded half, conic arcs fitted to SPARC for the X-point halves). Those are then fitted the same way.

## Name lettering (`emboss_names_solidworks.ps1` + `Emboss.cs`, run in SolidWorks)
- Arial Black capitals follow the top ring of the torus and read left to right when viewed from above.
- Each letter has a flat top 0.8 mm (4 × 0.2 mm layers) above the higher of two heights: the surface under it, and the top of the plasma. So the letters finish on a single clean top layer, and above the plasma's top layer only the lettering remains (see the two-colour printing table below).
- Letters extend straight down into the body and are merged with it, giving one solid body.
- The text sits on the band of the top surface where the surface drops least across the letter height.
- **SPARC and ARC:** these have an X-point ridge on top, so their text sits just outboard of the ridge.
- **NSTX-U:** its crown ring is tiny (R ≈ 7.6 mm at 1:100), so its text is moved just outboard, to R = 10 mm. There the end letters turn about 42° instead of the name wrapping around the hole. Its letter tops are set from the crown (11.19 mm + 0.8 mm), not from the slope under the text.

| Machine | Cap height (mm) | Text radius (mm) | Letter-top Z (mm) | Max surface drop under text (mm) |
|---|---|---|---|---|
| WEST   | 4   | 24.5 | 6.66  | 0.92 |
| EAST   | 4   | 17.5 | 7.21  | 1.05 |
| DIII-D | 4   | 15.5 | 10.47 | 0.80 |
| SPARC  | 4   | 16.6 | 12.15 | 2.17 |
| ITER   | 8   | 52.4 | 37.80 | 1.19 |
| ARC    | 6   | 40.7 | 22.03 | 2.45 |
| NSTX-U | 2.8 | 10.0 | 11.99 | 2.08 |
| JET    | 5   | 28.4 | 16.12 | 0.57 |
| JT-60U | 5   | 32.7 | 14.33 | 0.61 |

Re-import check of `step_named/`: every file is 1 solid body with 0 geometry faults.

**Two-colour lettering.** Everything above the plasma's top is lettering only. So add a filament change in the slicer at the first layer that starts at or above the height below. It takes 0.8 mm (4 layers at 0.2 mm) of letters in the second colour.
- **Which column:** use "Upright print" when the whole model is printed on supports, and "Top half print" when it is cut at Z = 0 and only the upper half is printed.
- **Measured from:** heights are from the bottom of the model. Add the raft height if you use one.

| Machine | Upright print: change at (mm) | Top half print: change at (mm) | Letter tops: upright / top half (mm) |
|---|---|---|---|
| WEST   | 11.97 | 5.86  | 12.77 / 6.66  |
| EAST   | 14.60 | 6.41  | 15.40 / 7.21  |
| DIII-D | 20.82 | 9.67  | 21.62 / 10.47 |
| SPARC  | 22.69 | 11.35 | 23.49 / 12.15 |
| ITER   | 74.00 | 37.00 | 74.80 / 37.80 |
| ARC    | 42.48 | 21.24 | 43.27 / 22.03 |
| NSTX-U | 24.41 | 11.19 | 25.21 / 11.99 |
| JET    | 30.03 | 15.32 | 30.83 / 16.12 |
| JT-60U | 26.33 | 13.53 | 27.13 / 14.33 |

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

**Files** (in `mirror_models/`, with the other mirrors; the CSV and trace overlay stay in `tokamak_models/`).
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

## Other mirrors: 2XIIB, GAMMA 10, GDT, TMX-U (`build_mirror_plaques_solidworks.ps1` + `MirrorSw.cs`)
**Source.** 1:100 flux-surface solids made by **Elliot Claveau**, in `mirror_models/step_raw/` (unchanged; GAMMA 10 is his corrected version of 18 Nov 2026). They are in mm, with the mirror axis along Z and the centre at the origin. Each imports into SolidWorks as 1 solid with 0 faults.

| Machine | Length (real / 1:100) | Largest radius (real / 1:100) | Plasma volume (real / 1:100) | Symmetry |
|---|---|---|---|---|
| 2XIIB | 6.28 m / 62.8 mm | 0.67 m / 6.7 mm | 0.195 m³ / 194.6 mm³ | quadrupole (min-B) throughout |
| GAMMA 10 | 21.3 m / 213.4 mm | 0.65 m / 6.5 mm | 1.114 m³ / 1113.6 mm³ | axisymmetric central cell, quadrupole anchors |
| GDT | 9.9 m / 99.0 mm | 0.23 m / 2.3 mm | 0.538 m³ / 538.1 mm³ | axisymmetric |
| TMX-U | 19.8 m / 198.0 mm | 2.50 m / 25.0 mm | 5.88 m³ / 5882.8 mm³ | axisymmetric central cell, quadrupole end cells and fans |

**How the plaques are made** (same form as the WHAM plaque):
- **Halving:** each solid is cut on its x = 0 plane, keeping x ≥ 0. That plane contains the axis, and it is a mirror-symmetry plane for all four machines, including the quadrupole sections. So the cut gives a true half (the kept volume is 0.500 of the full volume for every machine), with a flat face that sits on the plate.
- **Base and name:** a 2 mm plate, with the name raised 0.8 mm in 5 mm Arial Black. The name is centred under the middle of the machine, 2.1 mm clear of the relief.
- **What the quadrupole sections look like:** the plasma there is a thin ellipse that turns 90° from one section to the next. On the plaque, a section whose ellipse is wide in y lies flat as a sheet; one whose ellipse is wide in x stands up as a fin. In 2XIIB the two ends are one of each.

**Thickening to 0.8 mm (for a 0.4 mm nozzle; `MirrorThin.cs`).** At 1:100 parts of these plasmas are far too thin to print: down to 0.08 mm tall sheets and 0.15 mm wide fins in 2XIIB, 0.11 / 0.21 mm in GAMMA 10, 0.20 / 0.40 mm in TMX-U, and GDT's throats are only 0.26 mm in radius. So before halving, every part of the relief is brought up to at least 0.8 mm tall and 0.8 mm wide (two nozzle widths):
- **Finding the thin parts:** the solid is sliced every 0.25 mm along the axis (from its STL). At each slice the plaque relief is a = max x tall and 2b = 2 max |y| wide.
- **Thickening:** over each thin stretch (plus 1 mm either side), the real cross-section is stretched only in its thin direction: by 0.85 / a across the plate and 0.85 / 2b along it, aiming at 0.85 mm so the result stays above 0.8 between sections. In directions that are already thick enough it is shrunk to 85%, so it stays inside the real surface. These sections, every 1 mm, are lofted piece by piece and merged into the solid. So the shape is unchanged wherever it was already printable, and thin sheets and fins keep their outline but get thicker.
- **Seam at z = 0:** the solids are made of two halves meeting at z = 0, and SolidWorks won't merge a loft across it. So the 2XIIB thickening, the only one that crosses the centre, stops at z = 0 and restarts 0.001 mm past it. That 1 µm sliver is far below what a printer or slicer can resolve.
- **Result** (re-sliced after thickening): see the table below. The full-solid STLs in `stl/` are the true, unthickened shapes.
- WHAM's plaque is not thickened: its 0.2 mm-radius throats (2.3 mm long) are still 0.2 mm tall and 0.4 mm wide.

| Plaque (`step_named/` + `stl_named/<tag>_plaque_named_1to100`) | Plate (mm) | Relief height (mm) | Thinnest before → after thickening, height / width (mm) | Plasma volume, true → thickened (mm³) | Plaque volume (mm³) | Check |
|---|---|---|---|---|---|---|
| 2XIIB | 69.4 × 22.0 × 2 | 6.7 | 0.08 / 0.15 → 0.82 / 0.85 | 194.6 → 533.9 | 3371 | 1 solid, 0 faults |
| GAMMA10 | 220.0 × 19.4 × 2 | 6.4 | 0.11 / 0.21 → 0.85 / 0.85 | 1113.6 → 1483.9 | 9351 | 1 solid, 0 faults |
| GDT | 105.6 × 14.9 × 2 | 2.3 | 0.26 / 0.52 → 0.84 / 0.85 | 538.1 → 544.8 | 3460 | 1 solid, 0 faults |
| TMX-U | 204.6 × 54.6 × 2 | 25.0 | 0.20 / 0.40 → 0.85 / 0.85 | 5882.8 → 6070.4 | 25 424 | 1 solid, 0 faults |

2XIIB and GAMMA 10 gain the most volume because so much of them is thin sheets and fins: the plaque reliefs are true in outline, but their thin parts are thicker than the real plasma.

Other files: the full (true, unthickened) solids as STL in `stl/<tag>_plasma_1to100.stl`, and renders in `verification/<tag>_named_topview.png` and `_isoview.png`.

**Printing:** print flat, base down, with a 0.4 mm nozzle. TMX-U's tall end fin (25 mm high, about 6 mm thick at its base) may want a brim. The GAMMA 10 plaque is 220 mm long, so it needs a 220 mm bed, or a diagonal placement on a smaller one.

## Mirror comparison (`mirror_models/comparison_mirrors.svg`, `MirrorSvg.cs`)
All five mirrors at true scale (same scale along and across, 50 px per metre), stacked on a shared axial scale centred on each machine. Each outline is what its plaque shows from above:
- **WHAM and GDT:** the radial profile.
- **2XIIB, GAMMA 10 and TMX-U:** the plasma's extent across the plate (in y) at each axial position, sliced every 1 cm. The quadrupole sections show as wide sheets or narrow necks, depending on which way their ellipse points.

The outlines are of the true flux surfaces, not the thickened reliefs; thickening changes widths by at most about 4 cm real, less than 2 px. Ends run as on the plaques (TMX-U's wide fan on the right). It is redrawn at the end of every `build_mirror_plaques_solidworks.ps1` run.

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
| JET    | 1 | 6 | 0 | 73 443  | 73 441  |
| JT-60U | 1 | 6 | 0 | 68 691  | 68 681  |

Analytic volume = 2π × (centroid R) × (cross-section area), at 1:100 (1 m³ real = 1000 mm³).

## Acknowledgements
Many thanks to **Matthew Parsons** for providing the correct NSTX-U plasma shape and the files to generate it:
- `parsons_files/NSTXU_shape_generator.ipynb`: a TokaMaker NSTX-U shape generator and control-point selector.
- `parsons_files/omfit_shape_generator.py`: the OMFIT `boundaryShape` generator.

The NSTX-U model in this set is built from his equilibrium.

Many thanks to **Elliot Claveau** for the original 1:100 flux-surface STEP files of 2XIIB, GAMMA 10, GDT and TMX-U (`mirror_models/step_raw/`), from which those four mirror plaques are made.

## Sources
- EAST: Y.-Y. Chen et al., "Plasma shape optimization for EAST tokamak using orthogonal method", Chin. Phys. B 28 (2019) 015201, Fig. 1. (`EAST_paper.pdf`)
- DIII-D: G.L. Jackson et al., "Simulating ITER plasma startup and rampdown scenarios in the DIII-D tokamak", Nucl. Fusion 49 (2009) 115027, Fig. 1(b), 0.32 s. (`DIIID_paper.pdf`)
- NSTX-U: TokaMaker (OpenFUSIONToolkit) free-boundary LSN equilibrium from Matthew Parsons, `parsons_files/NSTXU_shape_generator.ipynb`:
  - **Inputs:** Ip 1 MA; target shape from the OMFIT `boundaryShape` (Luce 2013) generator.
  - **Figure used:** the saved plot is `../reference_figures/NSTXU_TokaMaker_separatrix.png`.
  - **Not re-run here:** the notebook needs the OFT install and `NSTXU_mesh.h5`.
  - **Previously used:** A. Pajares et al., Fus. Eng. Des. 220 (2025) 115302, t = 325 ms (`NSTXU_paper.pdf`).
- WEST: "WEST machine capabilities for 2024" (CEA), p. 1 figure. (`WEST_paper.pdf`)
- JET and JT-60U: P.C. de Vries et al., "Identity physics experiment on internal transport barriers in JT-60U and JET", Plasma Phys. Control. Fusion 51 (2009) 124050, Fig. 1, https://doi.org/10.1088/0741-3335/51/12/124050 (open access). (`JET_JT60U_paper.pdf`; figure at `../reference_figures/JET_JT60U_fig.png`)
- SPARC: A.J. Creely et al., "Overview of the SPARC tokamak", J. Plasma Phys. 86 (2020) 865860502, Fig. 2.
- ITER (separatrix κ 1.85, δ 0.49): ITER-FEAT design parameters, https://fire.pppl.gov/iter_feat_optimize.pdf
- ARC V3A (R0 4.62, a 1.18, κsep 1.8, δsep 0.65): arXiv 2606.09359 (Table 1) and arXiv 2608.10124.
- WHAM: D. Endrizzi et al., "Physics basis for the Wisconsin HTS Axisymmetric Mirror (WHAM)", J. Plasma Phys. 89 (2023) 975890501, Fig. 2.
- 2XIIB, GAMMA 10, GDT, TMX-U: flux-surface solids by Elliot Claveau (`mirror_models/step_raw/*_flux_surface_1to100.step`).

## Regenerating
From the `Tokamak Comparison` folder, in Windows PowerShell:
```
Add-Type -Path tokamak_models\TokamakGen.cs,tokamak_models\Trace.cs,tokamak_models\Figures.cs,tokamak_models\Build.cs -ReferencedAssemblies System.Drawing
[Build]::Run((Get-Location).Path)
```
`tokamak_models\check_step_solidworks.ps1` opens every STEP file in SolidWorks and reports body count, face count and volume.

`tokamak_models\emboss_names_solidworks.ps1` re-letters every machine; add `-Only NSTX-U` to re-letter one.

`tokamak_models\build_wham_solidworks.ps1` re-traces WHAM, rebuilds its solid and plaque in SolidWorks (written to `mirror_models/`), and adds it to the SVG.

`tokamak_models\build_mirror_plaques_solidworks.ps1` rebuilds the 2XIIB, GAMMA 10, GDT and TMX-U plaques from `mirror_models/step_raw/` (thickened to 0.8 mm) and redraws `mirror_models/comparison_mirrors.svg`; add `-Only GDT` (or `2XIIB`, `GAMMA10`, `TMX-U`) to rebuild one. 2XIIB and GAMMA 10 take several minutes each.
