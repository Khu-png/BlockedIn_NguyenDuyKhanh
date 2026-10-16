## Arrange by dragging

In Puzzle Assembly > Arrange, drag any occupied cell to move its whole group. The grabbed cell stays under the pointer and the preview snaps to the grid. Green means the whole group fits; red means the destination overlaps another group, a wall, or the board edge. Invalid drops leave the layout unchanged. Escape cancels the drag. Only the destination is checked, so pieces can be lifted across occupied areas. Drop onto one other group to swap their positions, provided both translated shapes fit. Cyan preview shows both resulting groups. Drops involving multiple target groups are rejected. Click-to-place also uses destination validation. Directional buttons have been removed. Undo/Redo and Save preserve colors, groups, ports and Rope/Ice settings. Manual placement does not certify that a level is solvable.

## Two level editors

- **Tools > Blocked In > Map Generator**: the original painting workflow, with manual Edit Shape / Merge / Split and color, port, Ice and Rope settings.
- **Tools > Blocked In > Puzzle Assembly**: build complete colored shapes, cut connected pieces with matching ports, then arrange them or shuffle using legal moves.

Each window keeps its own draft. Both use Save as Level / Save to write the level SO, build its runtime prefab and refresh LevelManager automatically. Existing level SOs can be loaded in either editor. Saving the same level number updates the same asset; use different numbers to keep separate levels.

# Puzzle Assembly

Open **Tools → Blocked In → Puzzle Assembly**.

In **Build Shape**, enable **Select Groups to Merge**, click each group, then **Merge Groups**. Clicking any cell selects the whole group; clicking it again deselects it. Groups must have the same color ID and their combined cells must be edge-connected. Disable selection mode to resume drawing separate stroke groups. Undo restores the original groups.

## Build, cut, arrange

1. **Build Shape**: choose Floor/Wall to define the board, then select Block and a Shape color. Paint touching cells to form the completed shape. Each press-drag-release creates one group. A new stroke stays separate even when touching an existing group of the same color. Entering Cut Pieces preserves these groups. Obstacle shapes can be painted, but cannot be cut into puzzle pieces.
2. **Cut Pieces**: click the exact cells to cut away from one piece, then press Cut Selected. Both resulting pieces must remain connected. The tool creates complementary Tab/Socket edges along the cut and preserves connections from earlier cuts. Repeat to create the desired pieces. Small yellow marks represent tabs; dark marks represent sockets.
3. **Arrange**: click a piece and then its destination for the first selected cell, or use the arrow buttons. Moves preserve shape, color, ports, and rope settings and cannot pass through walls, other pieces, or board edges. For a longer move, the tool checks horizontal-first and vertical-first paths; use intermediate destinations if neither path is clear.
4. **Shuffle with legal moves** performs up to the requested number of one-cell moves. It keeps obstacles, frozen colors, and roped pieces in place. It needs free floor space and may leave some matching pieces adjacent. Play-test the final layout, especially levels with Ice/Rope dependencies.
5. Right-click a piece in Cut Pieces or Arrange to open the existing color/ports/Ice/Rope popup.
6. Enter the level number and time limit, then use Save as Level. Save updates the selected Map data asset. Both create the linked runtime prefab automatically. Load opens existing levels directly in Arrange without changing their saved pieces or ports.

Undo/Redo supports cutting, arranging, and shuffling. Board prefab settings retains custom Tile/Backdrop/Wall assignments. GeneratedBoard remains the runtime layout component; manual scene-generation and camera-preview controls are hidden from this window.

CameraManager fits the saved runtime board to the gameplay area automatically when LevelManager loads it.

Mỗi ô có kích thước 1 unit trên XZ, hàng tăng theo -Z. Floor Y=-0.22, nền Y=-0.225, tâm tường Y=0.03. Block 1×1 đặt root Y=0.

Level SO stores board cells, colors, edges and merged groups. Save also bakes a runtime board prefab; LevelManager instantiates that prefab for playing, replaying and advancing levels. Each serialized LevelEntry contains the Level SO and its Time Limit. BoardWallCell is the current obstacle/border visual.


Chọn brush Block trong Map Generator rồi click/kéo ô để đặt block màu Blue. Click block để mở bảng màu; Remove Block giữ lại floor. Saving dựng block từ Block 1x1 Base, gán màu và input drag; Save/Load giữ vị trí và màu trong Level SO. Floor/Wall ghi đè block khi kéo qua ô; Fill Floor và Example xoá toàn bộ block trong bản nháp.

Click a block to edit its color and its Top/Right/Bottom/Left edges in the same popup. Select Default, Tab or Socket; click Done, then Save to apply the layout. Save/Load preserves edge choices in the Level asset. Older levels use Default edges.

Runtime docking preview: release a 1x1 block next to a same-ColorId block with opposing Tab/Socket edges. It slides into place and both visuals pulse; occupancy and colliders remain on the grid. Dock Duration, Dock Bounce Duration, Dock Gap and Dock Squash are configured on Block. The preview does not merge groups or remove blocks. Open BlockDockingDemo, drag the upper-left blue block next to the upper-right blue socket block and release. The lower blue/red pair demonstrates a color mismatch.
Runtime color completion: after startup or a release, once movement/docking is finished, all blocks with the same ColorId must form one connected component through opposite Tab/Socket pairs, with no unmatched special edge. A completed color is immediately deactivated, removed from occupancy, and destroyed; other colors remain. Plain single blocks and touching Default edges do not count as a completed connection. Checks run only in Play Mode; editor level data is preserved.


## Merged block shapes

Build Shape groups cells from the same paint stroke; releasing ends the group. Cut Pieces creates connected parts with matching ports. G labels identify merged pieces. Saving creates one gameplay Block with multiple cell visuals from Block 1x1 Base for each piece; Arrange moves all cells of that piece together.

Click a shape cell in Build Shape, or right-click it in Cut Pieces/Arrange, to open the palette. Color applies to the entire shape. Edge settings apply to the clicked cell; internal edges display Internal (Default). Merge resets internal Tab/Socket choices to Default. Generated shapes keep internal Default edges visible, replace rounded corners with CornerToStraight at all joins, and retain rounded outer corners. No Cover meshes are added: visible grooves between cells preserve the individual tile faces and leave exterior Socket/Tab parts exposed.

Level SOs store blockGroups alongside colors and edges. Older levels without groups load as 1x1 blocks. All occupied cells share one drag root, with explicit collider and appearance references per cell. Docking and color completion check exterior ports on every cell while counting each shape as one block.


## Mesh hitboxes

Block 1x1 Base uses a non-convex MeshCollider for pointer picking. The Map Generator bakes only active mesh parts after applying edge and corner choices. MeshFilter references are serialized in the base prefab and hidden in the normal Inspector; no component searches are used. Meshes are saved and reused in Assets/MainGame/Mesh/Hitboxes. Merged shapes retain one MeshCollider per occupied cell, all resolving to the same Block root; L-shaped gaps remain empty. Regenerate existing maps to replace their old BoxColliders. Grid collision and drag constraints still use Cells, not physics contacts.

## Drag assistance

Dragging checks both axis orders and selects the reachable position closest to the pointer. If the block is within 0.2 cells of a lane center and the pointer stays within 0.3 cells, dominant motion can align the block into that lane when alignment improves forward travel. Alignment and movement both check the entire footprint against walls, other blocks and board bounds. Open areas retain free dragging; moving the pointer farther sideways allows leaving a lane at an opening. Release snapping is unchanged. Test in Play with a one-cell corridor, slight sideways pointer movement, and a wider merged shape that must not fit the corridor.

## Gameplay, Win and Lose

Use the existing GameManager, LevelManager and UIManager scene components. Assign CameraManager, UI Events and the optional Editor Preview on LevelManager. Assign Gameplay, Win and Lose scene components or prefab components to UIManager's UI Resources array. Scene UI is reused directly; prefab UI is instantiated once and cached. SoundManager uses its two serialized AudioSources and clip tables. MainMenu, Pause, GridMap and CameraController are not part of this flow.

## Roped blocks

Click a block in the map palette, tick Roped and choose Rope Color. Enabling, disabling or changing this setting applies to every block with the same body Color ID, including separate groups. The grid adds an R marker. Rope Color lists only colors currently present on the board, excluding Obstacle and the locked block's own color. Completing the selected color cuts matching ropes and unlocks their blocks after the scissors animation. Any remaining ice continues to prevent movement.

Generate shows the rope preview; Save or Save as Level bakes the setting into the runtime prefab used by LevelManager. Group edits, resizing, save/load and replay preserve rope colors. Existing levels default to no ropes. The editor uses the original rope mesh/textures and converts the two scissors rotation curves from Cut.json into ScissorsCut.anim. Default materials, shape meshes and the clip live in Assets/MainGame/Prefabs/Rope; existing assets are reused.

Ropes cross diagonally over the occupied cells, including shaped groups. Scissors fly from the completed color along a curved path with a trail and glow, cut with a sparkle burst, then fly away. Open existing levels, Generate and Save to rebuild their runtime prefabs with the updated visuals.

LevelManager keeps a serialized LevelData array. Existing BoardMap assets inherit LevelData so previously saved levels remain compatible. Each Level SO stores its own Time Limit. In Map Generator, set Time limit (seconds) before Save or Save as Level; Load restores the selected level duration. You can also edit timeLimit directly in the SO Inspector. Save automatically adds the SO to registered scene LevelManagers, retaining custom assignments. Runtime board prefabs are prepared by Map Generator and linked inside the SO.

GameManager.Start calls OnInit, then LevelManager.OnPlay loads the saved level and opens Gameplay. Gameplay reads the level number and remaining time through LevelManager.Ins; assign its Level Text and Timer Text fields. Win/Lose are opened through UIManager.Ins.OpenUI<T>(). Connect button clicks to Gameplay.OnReplay, Win.OnReplay, Win.OnNextLevel and Lose.OnReplay. Assign Win's Next Button field so the final level disables Next.

The countdown waits for the first block press in each level attempt. Empty-floor and UI clicks do not start it. After starting, it continues after release; replay and level changes reset this gate. Zero time opens Lose. Removing all blocks with exterior Tab/Socket ports opens Win; plain blocks may remain. Results disable dragging. Replay reloads the current level and resets its timer. Next works only after Win and only when another level exists. Winning the final level still shows Win and preserves the final saved level number. No UI or scene creation tool is required.

Ready-made UI prefabs are in Assets/MainGame/Prefabs/UI: Gameplay, Win and Lose. Assign their UICanvas components to UIManager UI Resources and set CanvasParentTF to your scene Canvas. Use an Overlay Canvas with CanvasScaler Scale With Screen Size, reference resolution 1080 x 1920, and Match 0.5. Prefabs already contain text references, persistent Replay/Next listeners and a safe-area container. Gameplay BoardArea is passed to CameraManager when opened so the grid fits between the header and footer.
