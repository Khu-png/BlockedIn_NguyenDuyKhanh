# Map Generator

Mở **Tools → Blocked In → Map Generator** trong Unity.

1. Ba prefab `BoardTile`, `BoardBackdropCell`, `BoardWallCell` được nạp mặc định từ `Assets/MainGame/Prefabs/Board` khi mở tool. Bạn vẫn có thể gán prefab khác vào các trường tương ứng.
2. Chọn **Example 6×6** để tạo toàn bộ 36 ô sân, chưa có khối chặn; hoặc vẽ bằng cọ Floor/Wall. Ô mới mặc định là Floor. Dùng cọ Floor để xóa tường đã vẽ.
3. Nhấn **Generate** để dựng bàn trong scene đang mở. **Frame in Scene** giúp nhìn toàn bàn.
4. **Preview camera** tự lấy Main Camera đang bật, có tag `MainCamera`; không cần kéo thả. Nhấn **Fit Camera / View in Game** để chỉnh camera Orthographic nhìn xuống bàn và mở Game View. Nút này căn theo toàn bộ khung hình.
5. Nhập **Level number** rồi nhấn **Save as Level**. Tool tự lưu SO vào `Assets/MainGame/ScriptableObject/Levels/Level N.asset` (ví dụ `Level 5.asset`). Lưu cùng số sẽ cập nhật asset đó và giữ reference hiện có. Lần sau gán asset vào Map data, nhấn **Load**, rồi **Generate**. **Save** cập nhật asset đang được chọn.
6. Nhớ lưu scene để bàn và camera xuất hiện khi Play. Nếu đổi tỷ lệ Game View, nhấn Fit Camera lại.

Tool nạp prefab mặc định bằng đường dẫn asset cố định, không dùng Find/GetComponent hay tìm shader. Theo yêu cầu tự gán camera, preview dùng `Camera.main` và cập nhật theo scene đang mở.

Gắn `CameraManager` trong `Scripts/Manager` vào object Camera và gán Main Camera qua Inspector. Không cần có bàn sẵn: loader gọi `FocusLevel(generatedBoard)` sau khi dựng bàn, hoặc `FocusLevel(null)` khi rời level. Camera tự căn lại khi grid, transform bàn hoặc tỷ lệ màn hình thay đổi trong Play. Game Play Rect để trống sẽ dùng toàn màn hình; nếu có vùng UI dành cho bàn, gán RectTransform đó. UI Camera để trống với Canvas Overlay, hoặc gán camera render của Canvas. Khi chỉ thay layout UI, gọi `Fit()` lại.

Để test mà chưa có loader, tạo bàn bằng tool, chọn object có CameraManager, kéo bàn vào **Preview board (Editor)** và nhấn **Fit Camera**. Trường này chỉ phục vụ test trong Editor, không phải dependency của gameplay và không được lưu vào scene. Khi vào Play cần chọn lại Preview board và nhấn Fit Camera để mô phỏng loader; sau đó thay tỷ lệ Game View để kiểm tra camera tự căn lại. Thử grid 1×1, 4×12, 12×4 và 32×32, với Outer wall border bật/tắt. Bàn phải nằm trọn trong vùng chơi, còn một khoảng đệm quanh viền.

BoardMapWindow phụ trách giao diện và lưu/nạp dữ liệu. BoardSceneBuilder phụ trách dựng/xóa bàn. GeneratedBoard giữ reference trực tiếp tới ba nhóm sinh ra. Mỗi file dưới 250 dòng.

Generate cập nhật Generated board đang chọn; để trống trường này để tạo một bàn riêng. Clear chỉ xóa bàn được chọn. Generate, Clear và chỉnh camera hỗ trợ Undo. Không dùng khi Play hoặc đang mở Prefab Mode.

Mỗi ô có kích thước 1 unit trên XZ, hàng tăng theo -Z. Floor Y=-0.22, nền Y=-0.225, tâm tường Y=0.03. Block 1×1 đặt root Y=0.

Level SO stores board cells, colors, edges and merged groups. Save also bakes a runtime board prefab; LevelManager instantiates that prefab for playing, replaying and advancing levels. Each serialized LevelEntry contains the Level SO and its Time Limit. BoardWallCell is the current obstacle/border visual.


Chọn brush Block trong Map Generator rồi click/kéo ô để đặt block màu Blue. Click block để mở bảng màu; Remove Block giữ lại floor. Generate dựng block từ Block 1x1 Base, gán màu và input drag; Save/Load giữ vị trí và màu trong Level SO. Floor/Wall ghi đè block khi kéo qua ô; Fill Floor và Example xoá toàn bộ block trong bản nháp.

Click a block to edit its color and its Top/Right/Bottom/Left edges in the same popup. Select Default, Tab or Socket; click Done, then Generate to apply the layout. Save/Load preserves edge choices in the Level asset. Older levels use Default edges.

Runtime docking preview: release a 1x1 block next to a same-ColorId block with opposing Tab/Socket edges. It slides into place and both visuals pulse; occupancy and colliders remain on the grid. Dock Duration, Dock Bounce Duration, Dock Gap and Dock Squash are configured on Block. The preview does not merge groups or remove blocks. Open BlockDockingDemo, drag the upper-left blue block next to the upper-right blue socket block and release. The lower blue/red pair demonstrates a color mismatch.
Generated board binding uses a serialized editor ID and direct component registration. A lost ID falls back to the sole registered board in the active scene. Disabled/inactive boards remain registered until destroyed. If no board remains, stale references clear automatically so Generate can create a replacement. Multiple boards require an explicit assignment to avoid duplicates. Clear and Undo Clear restore the board and its tool reference.
Runtime color completion: after startup or a release, once movement/docking is finished, all blocks with the same ColorId must form one connected component through opposite Tab/Socket pairs, with no unmatched special edge. A completed color is immediately deactivated, removed from occupancy, and destroyed; other colors remain. Plain single blocks and touching Default edges do not count as a completed connection. Checks run only in Play Mode; editor level data is preserved.


## Merged block shapes

Paint Block cells, choose the same color ID, enable Edit Shape, select touching cells, then click Merge. G labels identify cells belonging to one merged block. Generate creates one gameplay Block with multiple cell visuals from Block 1x1 Base. Split restores 1x1 blocks. Remove Cells removes selected cells; if the remaining shape is disconnected, it becomes separate 1x1 blocks. Adding cells uses the normal Block brush followed by merging again; selecting any cell of an existing group includes that entire group when merging.

Disable Edit Shape and click a shape cell to open the palette. Color applies to the entire shape. Edge settings apply to the clicked cell; internal edges display Internal (Default). Merge resets internal Tab/Socket choices to Default. Generated shapes keep internal Default edges visible, replace rounded corners with CornerToStraight at all joins, and retain rounded outer corners. No Cover meshes are added: visible grooves between cells preserve the individual tile faces and leave exterior Socket/Tab parts exposed.

Level SOs store blockGroups alongside colors and edges. Older levels without groups load as 1x1 blocks. All occupied cells share one drag root, with explicit collider and appearance references per cell. Docking and color completion check exterior ports on every cell while counting each shape as one block.


## Mesh hitboxes

Block 1x1 Base uses a non-convex MeshCollider for pointer picking. The Map Generator bakes only active mesh parts after applying edge and corner choices. MeshFilter references are serialized in the base prefab and hidden in the normal Inspector; no component searches are used. Meshes are saved and reused in Assets/MainGame/Mesh/Hitboxes. Merged shapes retain one MeshCollider per occupied cell, all resolving to the same Block root; L-shaped gaps remain empty. Regenerate existing maps to replace their old BoxColliders. Grid collision and drag constraints still use Cells, not physics contacts.

## Drag assistance

Dragging checks both axis orders and selects the reachable position closest to the pointer. If the block is within 0.2 cells of a lane center and the pointer stays within 0.3 cells, dominant motion can align the block into that lane when alignment improves forward travel. Alignment and movement both check the entire footprint against walls, other blocks and board bounds. Open areas retain free dragging; moving the pointer farther sideways allows leaving a lane at an opening. Release snapping is unchanged. Test in Play with a one-cell corridor, slight sideways pointer movement, and a wider merged shape that must not fit the corridor.

## Gameplay, Win and Lose

Use the existing GameManager, LevelManager and UIManager scene components. Assign CameraManager, UI Events and the optional Editor Preview on LevelManager. Assign Gameplay, Win and Lose scene components or prefab components to UIManager's UI Resources array. Scene UI is reused directly; prefab UI is instantiated once and cached. MainMenu, Pause, SoundManager, GridMap and CameraController are not part of this flow.

LevelManager keeps a serialized LevelData array. Existing BoardMap assets inherit LevelData so previously saved levels remain compatible. Each Level SO stores its own Time Limit. In Map Generator, set Time limit (seconds) before Save or Save as Level; Load restores the selected level duration. You can also edit timeLimit directly in the SO Inspector. Save automatically adds the SO to registered scene LevelManagers, retaining custom assignments. Runtime board prefabs are prepared by Map Generator and linked inside the SO.

GameManager.Start calls OnInit, then LevelManager.OnPlay loads the saved level and opens Gameplay. Gameplay reads the level number and remaining time through LevelManager.Ins; assign its Level Text and Timer Text fields. Win/Lose are opened through UIManager.Ins.OpenUI<T>(). Connect button clicks to Gameplay.OnReplay, Win.OnReplay, Win.OnNextLevel and Lose.OnReplay. Assign Win's Next Button field so the final level disables Next.

The timer starts immediately. Zero time opens Lose. Removing all blocks with exterior Tab/Socket ports opens Win; plain blocks may remain. Results disable dragging. Replay reloads the current level and resets its timer. Next works only after Win and only when another level exists. Winning the final level still shows Win and preserves the final saved level number. No UI or scene creation tool is required.

Ready-made UI prefabs are in Assets/MainGame/Prefabs/UI: Gameplay, Win and Lose. Assign their UICanvas components to UIManager UI Resources and set CanvasParentTF to your scene Canvas. Use an Overlay Canvas with CanvasScaler Scale With Screen Size, reference resolution 1080 x 1920, and Match 0.5. Prefabs already contain text references, persistent Replay/Next listeners and a safe-area container. Gameplay BoardArea is passed to CameraManager when opened so the grid fits between the header and footer.
