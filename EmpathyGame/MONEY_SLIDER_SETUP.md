# Money sliders

Use the physical quill tip to move each paper's slider handle. Normal Unity UI dragging is disabled. Stamping locks the amount and commits it against the shared desk budget.

Open **Test with assets**, then run **Tools > Empathy Game > Set Up Family Desk Scene** outside Play mode and save. This exposes the repaired slider displays, per-object recovery points, return zones, and visitor routes for editing. Missing setup is also created automatically when Play starts.

The sliders now live on separate, uniformly scaled world-space canvas roots and follow their paper's position. Adjust **PaperSliderVisual > World Offset / World Euler Angles / Meters Per Pixel** to tune placement and size. Their quill trigger colliders are centered on the handles. The saved scene no longer uses OVROverlayCanvas on these sliders.

For new papers, select the PaperDocument object and use **Tools > Empathy Game > Add Money Sliders to Selected Papers**. The tool creates a quill-controlled slider with its handle trigger. The quill still needs its existing QuillTip marker on its tip collider and a Rigidbody on its grabbable root.

See [the complete family desk setup guide](DESK_SYSTEM_SETUP.md) for the shared budget, deliberate paper returns, dialogue and conditional interactions, respawning objects, stamper animation, and VR appearance checks.
