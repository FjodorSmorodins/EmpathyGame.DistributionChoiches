# Money allocation sliders

1. Open `Test with assets` in Unity and wait for scripts to compile.
2. Select the paper GameObjects that have `PaperDocument` (multi-selection works).
3. Run **Tools > Empathy Game > Add Money Sliders to Selected Papers**.
   Existing sliders are skipped. The command creates one world-space slider per
   selected paper and uses Meta's setup wizards to add poke/ray canvas interaction,
   an input module, and rig interactors as needed. Save the scene afterward.
4. Adjust each generated **Money Slider** position/rotation above its paper so it
   faces the player and doesn't overlap the paper or stamp. It follows its paper.
   No hand-written collider setup is required for these UI sliders.
   `PaperMoneySlider` automatically adds a `PaperSliderGrabGuard` to the UI slider.
   It temporarily blocks grabbing an unheld paper while a pointer is over the
   slider or dragging it, then restores grabbing when the pointer leaves. An
   already-held paper is left alone so the other hand can adjust the amount.
5. On each **PaperDocument**, set **Maximum Amount**, **Amount Step**, and
   **Starting Amount**. Defaults are 3000, 100, and 0. On **PaperMoneySlider**, set
   **Currency Symbol** if you want something other than euros.
6. Disable or delete the **Ink Pads** scene objects. They no longer do anything.
   Keep the stamper, its grab components, and its existing stamp-head trigger.
7. Optionally assign a neutral **Stamped Texture** on each paper. Without one,
   the paper texture stays unchanged; the grey slider and "Locked" label still
   confirm stamping. Old red/yellow/green texture fields are no longer used.

Drag the handle to select money, then touch the paper with the stamper. The amount
is stored on `PaperDocument.AssignedAmount`. Stamping locks both the data and UI,
including rejecting late drag callbacks and repeat stamps. `Stamped` now sends
the paper and its integer amount. `ResetPaper` restores its initial amount and
unlocks the UI. The existing automatic return sequence is unchanged.

This mechanic stores independent allocations. It does not yet debit a shared
budget or connect to the older `ResourceDistributionManager` scoring system.

## Play-mode checks

- Drag each paper's slider; only that paper's amount should change.
- Stamp without touching any ink pad; verify the amount locks and the UI greys out.
- Continue dragging or stamp again; the locked amount must remain unchanged.
- Stamp while a slider drag is active; the value at stamping must remain locked.
- Check 0, maximum, and a maximum not divisible by the step (e.g. 250 with step 100).
- Reset the paper; its starting amount and editable colours should return.
- In the headset, test finger poke and controller/hand ray dragging. Confirm the
  canvas faces the player and does not obstruct grabbing or stamping the paper.
