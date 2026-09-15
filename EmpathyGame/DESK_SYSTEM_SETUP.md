# Family desk setup

The saved **Test with assets** scene is connected to the new system. Babushka is first, then Bakers, then Farmers. Each uses the paper and lane that already belonged to that family. Sample dialogue is editable placeholder text, with optional audio clip fields; no recorded voices are included.

## First use

1. Let Unity finish importing and compiling. Open **Assets/Scenes/Test with assets.unity**.
2. Outside Play mode, choose **Tools > Empathy Game > Set Up Family Desk Scene**, then save. This makes the generated setup visible and editable in the Hierarchy. Play mode also creates missing setup automatically.
3. Under **Family Desk System > Desk Setup Points**, check the **Entrance**, **Stand**, **Exit**, **Player Return Zone**, and individual **Respawn** points. Move the route and respawn points to the positions you want. The capsule floor height starts at world Y **3.2** as a provisional value for this elevated booth; check it against your floor mesh.
4. Press Play. Babushka approaches, speaks, and sends her paper across. Use the quill on the slider handle, hold the stamper and touch its head to the paper, then return the paper to its original player slot and release it. After the goodbye, the representative leaves and the next visit starts.

The setup command fills missing references. It does not overwrite routes or respawn components you have already configured. It marks the scene dirty rather than saving over your work automatically.

## Paper returns and colliders

Stamping locks and spends the selected amount. It no longer sends the paper back immediately.

The return check requires a fresh hand release after stamping, over the correct player slot, followed by the paper resting inside the return zone for **0.25 seconds**. You can release up to **0.25 m above** the zone and let the paper land. A small lift is enough; you do not need to move the paper completely outside the zone. The other family's slot, a tracking cancellation, or an automatic respawn does not count. The budget/instructions display shows what the return check is waiting for; the zone's **Return Status** also shows it in the Inspector during Play.

**You do not need to add colliders to the three existing slots manually.** Setup creates a separate, unscaled **Box Collider** with **Is Trigger** enabled at each existing PlayerSlot position. Select a **Player Return Zone** and use the collider's **Edit Collider** tool if its detection area needs adjustment. The cyan outline shows the accepted area; the paper's origin must be inside it. The generated zone is independent of the very thin slot marker, so moving that marker later also requires moving the zone.

Keep each paper's existing Rigidbody, physical collider, and Meta Grabbable. PaperGrabState now subscribes to the actual Meta hand-grab events; no Inspector grab/release event wiring is necessary. Sliding temporarily suppresses grabbing and restores the previous component states when finished.

## Money

Select **Family Desk System** and change **Starting Budget** (default **3000**) before Play.

All families share that amount. Adjusting a slider previews an allocation; stamping commits it exactly once. The next paper's maximum is the smaller of its own **Maximum Amount** and the remaining shared budget. A zero allocation can still be stamped when no money remains. Respawning a paper never refunds money or unlocks its stamp.

Each turn must reference its own PaperDocument. Reusing the same document for multiple turns is rejected, so a reset cannot accidentally spend from the same paper twice.

## Visit order and walking

On **Family Desk System**, expand **Family Turns**. Reorder or add entries and assign each entry its paper, lane index, and **Visit** asset. Lane indices start at 0.

The supplied assets are in **Assets/Desk Content**:

- **Babushka Visit**, **Bakers Visit**, **Farmers Visit**: name, capsule colour, walking speed, arrival delay, optional prefab, and dialogue sequences for arrival, stamping, and return.
- **... Arrival** and **... Goodbye**: the dialogue content.
- **Babushka Trust** and **Babushka Special**: example keys for conditional content and interactions.

Use **Create > Empathy Game > Family Visit** for more definitions. Leave **Representative Prefab** empty for a capsule. Future NPC prefabs should have their origin at their feet.

Each lane has an entrance, standing point, exit, and optional **Approach Waypoints**. Movement follows those points directly; it does not do obstacle avoidance. Place waypoints around geometry. Changing **Floor Y** affects newly generated defaults, so adjust existing route transforms directly after setup.

**Start Automatically** controls the first visit. **Automatically Start Next Family** controls progression after each visitor leaves. Disable it and call **StartNextFamily()** from a hand interaction's UnityEvent to control progression yourself. A Visit asset's **Arrival Variable / Arrival Value** can also gate its arrival until a story variable matches.

The **On Family Started**, **On Family Finished**, and **On All Families Finished** events allow more scene interactions. Started fires when the representative reaches the booth; Finished fires after the returned paper and goodbye dialogue. FamilyDeskManager exposes **CurrentRepresentative** and **CurrentPaper** for future scripts.

## Dialogue and special interactions

Create a **Dialogue Sequence** through **Create > Empathy Game** and add as many steps as needed:

| Step kind | Purpose |
| --- | --- |
| Line | Show the speaker and text, optionally play a Voice clip. |
| Set Variable | Set a Story Variable asset's runtime value. |
| Interaction | Invoke the matching scene action on DialogueRunner. |
| Wait For Variable | Pause until the specified variable equals Value. |
| Delay | Wait for Seconds. |

A line stays up for at least **Seconds**, or the duration of its voice clip if longer. **Wait For Advance** additionally waits for **DialogueRunner.AdvanceDialogue()**. Connect that method to a hand-based button or other interaction if desired. The default lines advance automatically and require no pointer/controller UI.

Any step can have a **Condition Variable / Condition Value**. It is skipped unless the values match when that step is reached. For example, the third line in **Babushka Arrival** only appears when **Babushka Trust** is **1**. Set the asset's **Initial Value** to 1 before Play to try it, or use a Set Variable step earlier in the sequence.

For a custom action between lines:

1. Create a **Story Interaction** asset (or use **Babushka Special**).
2. On the scene's **DialogueRunner**, add an **Interactions** entry with that asset. Connect **On Triggered** to an animation, object activation, your own component method, etc.
3. Add an **Interaction** step to the dialogue with the same asset. Set its optional condition if needed.
4. If you enable **Wait For Interaction**, the action must call **CompleteInteraction** on DialogueRunner with that same asset when done. Otherwise the conversation intentionally waits. Missing bindings produce a warning and are skipped.

To react whenever a variable changes, even outside a dialogue sequence, add **StoryConditionReaction** to a scene object. Assign the DialogueRunner, variable, expected value, and **On Matched** event or interaction asset. **Only Once** controls repeat reactions. **Check On Enable** also checks its starting value.

To change a value from a hand interaction, add **StoryVariableAction**, assign its **Story**, **Variable**, and **Value**, and connect the interaction's event to **Apply()**. The variable's value is stored in the DialogueRunner for this play session; the ScriptableObject asset is not modified by gameplay.

## Fallen objects

Setup adds **ImportantObjectRecovery** to the existing papers, stamper, quill, and other Meta Grabbables with a Rigidbody in this scene. Each gets its own named **Respawn** transform.

- Position each point above a safe part of the table, with enough clearance for the object's collider.
- Set **Fall Below Y** on each object's recovery component to a world height below the tabletop. The initial threshold is 0.45 m below its setup height.
- Held objects and papers undergoing an automatic slide are not teleported.
- A recovery clears velocity and restores the chosen pose, preserving allocation/stamp state.
- For an important object created later at runtime, add **ImportantObjectRecovery** to the same root as its Rigidbody and Grabbable, and assign its own respawn point.

## Stamper

The existing controller entered Stamp with no condition. **Idle > Stamp now requires the Stamp trigger**, and Stamp returns to Idle after the full clip. The tool triggers it only after a valid stamp, with root motion disabled and animation culling disabled. The stamp head is explicitly connected to the tool, and held contact is checked continuously so it works if contact already exists when the amount becomes editable.

To inspect the imported animation, use **Tools > Empathy Game > Inspect Stamper Animation**. If it reports curves on the grabbed root, use **Bake Stamper Visual Animation**. That makes editable press/rest clips with root transform curves removed, keeping the imported file unchanged. The controller then uses those clips. This is optional; the trigger fix already applies to the saved controller.

Check the result in VR: the model's moving parts should animate while the grabbed root follows your hand. If the model appears static despite the state changing, inspect the reported clip paths against the model hierarchy. Avoid renaming animated model children without updating the clip bindings.

## Slider appearance in VR

The old sliders combined nonuniform paper scaling, rotation, compositor canvases, and large offsets on their handle colliders. The repaired setup:

- Uses one ordinary world-space Canvas per paper. The three OVROverlayCanvas components were removed from the saved scene.
- Keeps the display on a separate, uniformly scaled root, with a fixed world tilt, following the paper's position.
- Places it 0.14 m above the paper, resets internal rotation/depth offsets, and centers the quill trigger on its visible handle.
- Uses larger text and a 4 cm-wide handle trigger. The quill remains the input method.

Select the separate **Money Slider** object and adjust **PaperSliderVisual > World Offset**, **World Euler Angles**, or **Meters Per Pixel**. Default scale 0.001 gives a 30 cm-wide display. Increase it modestly if text is still too small at your working distance. Use a shallower tilt if the panel is too angled; it intentionally does not rotate every frame to face the headset, which would move the quill target during interaction.

Keep a visible gap between the paper mesh and the UI to avoid intersecting surfaces. Keep a single render path while testing; re-adding an overlay requires its own scale, layer, and render-resolution setup. The current scripts do not change the simulator or dynamic-resolution settings that were previously fixed.

## Checks

The runtime scripts and editor setup scripts compile against the installed Unity/Meta SDK assemblies. There are 27 executable regression checks for budget spending and deliberate paper returns:

```text
dotnet run --project Tools/DeskLogicChecks/DeskLogicChecks.csproj
```

In Play mode, also check: a wrong-slot release does not submit; a held paper does not submit; a recovered stamped paper stays locked and can subsequently be returned; spending 3000 leaves the other families able to receive 0; each capsule visits its assigned lane; changing Babushka Trust to 1 enables her conditional line. Headset appearance and physical hand interaction still need a VR pass.
