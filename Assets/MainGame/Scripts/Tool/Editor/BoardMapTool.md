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

SO hiện lưu sân/tường, chưa lưu khối chơi, điều kiện ghép hay thời gian màn. Scene đã Generate xem được trong Play, nhưng chưa có loader runtime để tự đọc SO khi đổi level. BoardWallCell dùng cho ô tường/viền thử nghiệm, chưa có viền bo liền và glow như ảnh game.
