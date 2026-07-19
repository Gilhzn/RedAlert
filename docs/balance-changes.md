# Balance & Design Changes vs. the Original Baseline

Every deliberate deviation from the researched original-engine behavior (docs/research/) is recorded here, with rationale. Anything not listed is intended to match the baseline.

| # | Area | Original behavior | Our change | Rationale | Phase |
|---|---|---|---|---|---|
| 1 | Camera | Fixed isometric, no rotation | Optional 90° rotation steps + smooth zoom | Modern 3D affordance; readability preserved | 2 |
| 2 | Sidebar | Two cameo columns, no tabs | Tabbed sidebar (structures/defense/infantry/vehicles/air) | RA2-style evolution, better discoverability | 3 |
| 3 | Group movement | Per-unit A*, clumpy | Flow fields for group orders + separation steering | Modern pathing feel | 1-2 |
| 4 | UX | — | Attack-move, multi-queue rally, control-group recall UX | Modern RTS conventions | 4+ |
| 5 | Jump troopers | Jumpjet locomotor (wobble, cruise height) | Treated as light aircraft | Simplification; revisit in polish | 5 |
| 6 | Defenses | Engage within sight | Structures acquire at weapon range (artillery emplacements outrange sight) | Playability | 5 |
| 7 | Storage | Money hard-capped by silos | Base treasury floor = starting credits; silos extend beyond it | Early-game playability | 3 |
