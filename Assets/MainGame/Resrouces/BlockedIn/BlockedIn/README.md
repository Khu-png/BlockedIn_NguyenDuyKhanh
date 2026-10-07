# Blocked In – trích xuất âm thanh, hình ảnh, model 3D và asset

*Ngày 2026-10-07. Nguồn: `D:\ExtractApk\Blocked In.apk` (73,7 MB).*

| Mục | Giá trị |
|---|---|
| Package | `com.tripledot.blockorder` (Tripledot), versionName `0.05.00`, versionCode 104, minSdk 25, targetSdk 36 |
| Engine | Unity **6000.3.11f1**, URP, IL2CPP (`global-metadata.dat` có trong APK, `libil2cpp.so` thì không) |
| Nội dung | 4 scene (`StartUpScene`, `IntroScene`, `LobbyScene`, `GameplayScene`) + 2 bundle Addressables 2.9.1 đặt sẵn trong APK (`app_assets_all`, `gameplay_assets_all`) |
| Split APK | Manifest chỉ yêu cầu split `base__abi` (thư viện native), nên **toàn bộ asset nằm trong APK gốc**, không cần kéo thêm split từ máy ảo MuMu |
| Texture | ASTC 6×6 / 4×4, RGBA32, Alpha8…; tất cả 313 texture decode được |
| Âm thanh | FSB5 Vorbis, mono, 48 kHz (nhạc nền và tiếng bấm nút 44,1 kHz) |
| SDK | AppLovin MAX + nhiều mạng quảng cáo (Pangle, Mintegral, Vungle, InMobi, Chartboost, Fyber, BigoAds, Moloco, Ogury, PubMatic, Verve, BidMachine, MobileFuse, Magnite, Meta Audience Network, Unity Ads, Amazon), Adjust, Firebase |

Hầu hết asset có hai bản giống nhau: một bản trong scene build sẵn (`sharedassets*.assets`), một bản trong bundle Addressables. Mỗi asset chỉ được giữ một bản.

## Cấu trúc thư mục

```
BlockedIn/
├─ README.md
├─ sounds/ + sounds_index.csv     26 âm thanh Ogg Vorbis
├─ images/                        317 PNG + ảnh tổng hợp (contact_sheet_*.png) + images_index.csv
│  ├─ game/<nhóm>/                171 ảnh của game, chia theo nhóm (bảng bên dưới)
│  ├─ engine_sdk/                 146 ảnh của Unity, render pipeline, SDK, công cụ debug
│  ├─ sprites/                    16 sprite cắt từ atlas (đều là icon của bảng debug console)
│  └─ levels/                     preview 100 level có sẵn + 26 level local, kèm ảnh tổng hợp
├─ models/                        45 mesh → GLB + OBJ, prefab, khối đã ghép, texture đã "nướng màu"
├─ fonts/                         6 TTF
├─ data/                          level, config, ScriptableObject, bảng sự kiện âm thanh, animation, shader
├─ scripts/                       script dùng để trích xuất (chạy lại được)
└─ extracted/apk/                 assets/ + AndroidManifest của APK giải nén (60 MB)
```

## 1. Âm thanh – `sounds/`

26 file, tổng 1 phút 36 giây. Có 52 AudioClip, nhưng mỗi clip xuất hiện hai lần (scene + bundle Addressables) nên còn 26 file sau khi lọc trùng. Tất cả đã kiểm tra: giải mã được, đúng độ dài, không có file im lặng.
Cột "sự kiện" lấy từ 27 ScriptableObject `AudioEntry` của game (`data/audio_events.csv`). Đây là tên sự kiện trong code gọi tới âm thanh đó. Riêng `sfx_open` không gắn clip nào.

| File | Độ dài | Sự kiện trong game |
|---|---|---|
| `BO_BGM_Gameplay_02_Loop.ogg` | 53.83 s | in_game_music (loop, volume 0.7) |
| `BO_SFX_Coins_FlyToWallet_01.ogg` | 2.07 s | target_collected |
| `BO_SFX_Core_2BlockPut_02.ogg` | 0.16 s | piece_drop |
| `BO_SFX_Core_3BlockMatch.ogg` | 0.85 s | piece_match |
| `BO_SFX_Core_4BlockBreak_01.ogg` | 0.37 s | piece_break |
| `BO_SFX_IceBlock_Blocked_02.ogg` | 0.49 s | ice_movement_block |
| `BO_SFX_IceBlocksBreak_small_02.ogg` | 0.99 s | ice_small_break |
| `BO_SFX_IceBlocksBreak_whole_02.ogg` | 0.90 s | ice_whole_break |
| `BO_SFX_LevelLose_1OutOfTime_01.ogg` | 1.22 s | out_of_time |
| `BO_SFX_LevelLose_2LevelFailed_01.ogg` | 2.17 s | level_lose |
| `BO_SFX_LevelWin_1LevelComplete_01.ogg` | 3.46 s | level_complete |
| `BO_SFX_LevelWin_2Amazing_01.ogg` | 3.35 s | level_win_amazing |
| `BO_SFX_LivesEmpty_01.ogg` | 2.75 s | lives_empty |
| `BO_SFX_LivesRefill_01.ogg` | 1.82 s | lives_refill |
| `BO_SFX_LowTimeWarning_oneshot_01.ogg` | 1.00 s | low_timer_warning (loop) |
| `BO_SFX_PiecesSnapTogether_01Snap_01.ogg` | 1.46 s | piece_snap |
| `BO_SFX_PU_HammerHit_01.ogg` | 2.19 s | block_break_hit (búa) |
| `BO_SFX_PU_Selection_01.ogg` | 1.64 s | block_break_selection |
| `BO_SFX_ReviveTrigger_01.ogg` | 2.67 s | revive_trigger |
| `BO_SFX_RewadClaimxN_01.ogg` | 2.44 s | rewarded_video_claim |
| `BO_SFX_Ropes_Locked_01.ogg` | 0.15 s | ropes_movement_block |
| `BO_SFX_RopesScissorCut_01ScissorShow_01.ogg` | 0.31 s | ropes_scissor_show |
| `BO_SFX_RopesScissorCut_02ScissorCut_01.ogg` | 1.23 s | ropes_scissor_cut |
| `BO_SFX_StationaryBlock_01.ogg` | 0.28 s | immovable_movement_block |
| `BO_SFX_TimeFreeze_01.ogg` | 7.34 s | time_freeze |
| `Button Click.ogg` | 0.38 s | button_pressed |

Đã thêm tab "Blocked In" vào trang nghe thử chung `D:\ExtractApk\sound_player.html`.

## 2. Hình ảnh – `images/`

Ảnh gốc là texture nén ASTC, đã giải mã ra PNG giữ kênh alpha. `images_index.csv` ghi mỗi ảnh: kích thước, định dạng nén gốc, file nguồn và những file khác cũng chứa ảnh đó.
Mỗi nhóm có một ảnh tổng hợp `contact_sheet_<nhóm>.png` (nền ô caro là vùng trong suốt).

| Thư mục `game/` | Số ảnh | Nội dung |
|---|---|---|
| `ui/` | 63 | Nút, panel, popup, icon (tim/mạng, coin, rương, shop, home, leaderboard, cài đặt, khóa…) |
| `blocks/` | 35 | `Block_GradientMap` (ảnh xám dùng để tô khối), 12 `ColorRamp_*`, 12 `Symbol_*` (biểu tượng in trên mặt khối), mũi tên di chuyển, StationaryBlock |
| `ice/` | 24 | Khung băng UI (góc, cạnh, bản SDF), map gradient của khối băng, ảnh phản chiếu, normal map vết xước |
| `powerups/` | 11 | Búa phá khối, đóng băng thời gian (bản thường + bản khóa), nút cộng/số lượng |
| `vfx/` | 13 | Glow, sparkle, khói, hào quang nổ… |
| `branding/` | 9 | Logo "Blocked In" 2048×1173, nền splash 1080×1920, icon app (bản vuông/tròn, nền + tiền cảnh adaptive) |
| `mechanic_icons/` | 5 | Icon giới thiệu cơ chế: dây, khóa trục, lồng, băng, khối chặn |
| `rope_scissors/` | 5 | Texture dây thừng, bóng dây, kéo |
| `rewards/` | 4 | Coin, mạng (bản thường + SDF) |
| `fonts/` | 2 | Atlas SDF của font Baloo 2 (Medium, ExtraBold) |

`engine_sdk/`: `unity_builtin/` (ảnh mặc định của Unity), `render_pipeline/` (blue noise, film grain, SMAA của URP), `sdk_and_tools/` (icon của SDK quảng cáo, bảng debug), `debug_tools/` (IngameDebugConsole, UnityDebugSheet).

### Preview level – `images/levels/`

`builtin/level_001.png … level_100.png` và `local/Level_XX_*.png`, cùng ảnh tổng hợp 25 level một tấm. Ảnh do script vẽ từ dữ liệu level:

- Ô xanh đậm là bàn chơi, ô xám bo góc là tường.
- Khối được tô bằng màu giữa của ColorRamp và in biểu tượng màu của nó. Khối chặn (`blocker`) là khối xám có dấu ✕.
- Mũi tên: khối chỉ trượt được theo một trục.
- Lớp mờ trắng kèm số: khối bị băng, số là `iceCount`.
- Vạch màu: dây khóa (`lockColor`). Huy hiệu "K": chìa khóa (`keyColor`). "pin": khối cố định.
- Cột bên phải là các `orders`, tức hình mục tiêu cần ghép.
- Hàng 0 được vẽ ở trên cùng. Đây là quy ước tôi chọn; dữ liệu không cho biết game đặt hàng 0 ở đâu.

## 3. Model 3D – `models/`

Khối trong game không phải một model nguyên khối. Lúc chạy, code ghép khối từ các mảnh nhỏ theo từng ô lưới. Danh sách mảnh lấy từ các ScriptableObject `PieceBlocksParts`, `BoardBlocksParts`, `BackdropParts`, `StationaryBlocksParts` và `RopeParts` (`data/mesh_part_sets.json`).

| Nhóm (`glb/<nhóm>/`, `obj/<nhóm>/`) | Mesh | Ghi chú |
|---|---|---|
| `piece_block` | 21 | Khối người chơi kéo: `FrontFace` (mặt trên), `Edge_*`, `Corner_*`, `CornerToStraight_*`, `Cover_*` (nối 2 ô liền nhau), `Tab_*`/`Socket_*` (mấu và ổ khớp), `Tab_Ice` (vỏ băng của mấu) |
| `board_wall` | 10 | Tường của bàn: `Base_*`, `Corner_*` (lồi, lõm, tròn), `Padding` |
| `board_backdrop` | 3 | Nền bàn: `BackgroundFull`, `BackgroundConvex`, `BackgroundConcave` |
| `stationary_block` | 4 | Khối kim loại cố định, 4 góc |
| `rope` | 3 | Dây khóa: `Rope_Segment`, `Rope_End_01/02` |
| `powerup_tools` | 3 | `BlockHammer` (1196 đỉnh), `Scissor_01/02` (2 lưỡi kéo) |
| `board_tile` | 1 | `Rounded_Square` – ô nền bàn |

- **`glb/`**: mỗi mesh một file GLB, đã kèm texture, mở được bằng Windows 3D Viewer, Blender, three.js… Cả 61 file GLB (gồm prefab và khối đã ghép) qua trình kiểm tra Khronos glTF-Validator 2.0.0-dev.3.10 với **0 lỗi, 0 cảnh báo**.
- **`obj/`**: cùng các mesh ở dạng `.obj` + `.mtl`, texture nằm trong `obj/textures/`.
- **`prefabs/`**: `Scissors` (2 lưỡi, bóng, glow, giữ nguyên cây node), `PowerUp_Hammer`, `TileTemplateA/B`.
- **`assembled/Block_1x1_<Màu>.glb`**: khối 1 ô ghép từ 9 mảnh (`FrontFace` + 4 `Edge` + 4 `Corner`, đặt tại gốc tọa độ đúng như trong file mesh), đủ 12 màu của palette `BlockOrderPalette`.
- **`textures/`**: texture đã nướng màu sẵn. `Block_<Màu>.png` là `Block_GradientMap` tra qua `ColorRamp_<Màu>`, `Rope_<Màu>.png` dùng màu dây trong palette, ngoài ra có `Ice_baked.png` và `ScissorsHandle_Black.png`. Dùng để đổi màu khối trong Blender.
- **`previews/`** và `contact_sheet_meshes.png`, `contact_sheet_prefabs_blocks.png`: ảnh render từng mesh.
- `models_index.csv`: số đỉnh, số tam giác, kích thước, material Unity gốc của từng mesh.

Quy đổi tọa độ: Unity dùng hệ tay trái, còn glTF/OBJ dùng hệ tay phải. Khi xuất, trục z được đảo dấu và thứ tự đỉnh tam giác được đảo lại (cùng cách làm với UnityGLTF). Một ô lưới có kích thước 1 × 1, trục y hướng lên.

## 4. Dữ liệu – `data/`

| Đường dẫn | Nội dung |
|---|---|
| `levels/builtin/level_001…100.json` | 100 level có sẵn trong APK, lấy từ 2 file level pack. Bản gốc dạng JSON-lines nằm trong `levels/packs/` |
| `levels/local/Level_01_Tutorial … Level_26_AllRopeColors.json` | 26 level `LocalLevelConfig`, có vẻ là bộ level để test hoặc trình diễn cơ chế |
| `levels/levels_index.csv` | Mỗi level: kích thước lưới, thời gian, số khối, số order, số tường, layout id, các cơ chế xuất hiện |
| `config/` | `config.json` (chapter 1 = level 1–100), `RemoteConfig.json` (URL CDN Tripledot cho bản dịch, chapter set, level chest), cấu hình AppLovin, Adjust, billing… |
| `scriptable_objects/` | 84 ScriptableObject đọc từ bundle (bundle có type tree), mỗi tham chiếu đã ghi kèm tên đối tượng (`_ref`). Đáng chú ý: palette 12 màu, `PieceVisualConfig`, `BlockBreakVisualConfig`, `ScissorsCutConfig`, `CameraAnimationConfig`, các nội dung hướng dẫn, `AudioCatalog` |
| `audio_events.csv` | 27 sự kiện âm thanh → clip, loop, volume, mixer group |
| `mesh_part_sets.json` | Mảnh nào giữ vai trò gì (vd. `_cornerTopLeft` → `Corner_TopLeft`) và dùng material nào |
| `animations/` | 73 AnimationClip UI dạng JSON type tree (`Hammer_Hit`, `TimeFreeze_On`, `Win_*`, `PlayOn_*`…) |
| `shaders.txt` | 191 tên shader, gồm các shader riêng `BlockOrder/*` |

Cấu trúc một level: `cols`, `rows`, `time` (giây), `walls` (danh sách ô `[col,row]`), `shapes` (khối: `key`, `col`, `row`, `cells`, `color`, cùng các trường tùy chọn `movement`, `iceCount`, `lockColor`, `keyColor`, `obstacle`/`pinned`), `orders` (hình mục tiêu ghép từ các `parts` theo `shapeRef`).

Cơ chế trong 100 level có sẵn, kèm level đầu tiên có cơ chế đó: tường 58 level (từ level 5), băng 42 (từ level 8), khối chặn 47 (từ level 13), dây khóa 14 (từ level 27), chìa khóa 1 (level 28), mũi tên khóa trục 25 (từ level 40). Thời gian mỗi màn từ 60 đến 210 giây.

## 5. Font – `fonts/`

Có 6 file TTF: `Roboto-Regular/Bold`, `LiberationSans`, `NotInter-Regular`, `PerfectDOSVGA437`, `LegacyRuntime`.
Font chính của giao diện là **Baloo 2**, nhưng game chỉ đóng gói nó dưới dạng TextMeshPro SDF: ảnh atlas nằm trong `images/game/fonts/`, bảng glyph trong `data/scriptable_objects/TMP_FontAsset__*.json`. Không có file TTF gốc của Baloo 2.

## 6. Chạy lại

```bash
python D:/ExtractApk/_sound_tools/extract_sounds.py BlockedIn
python D:/ExtractApk/_sound_tools/validate.py BlockedIn
python D:/ExtractApk/BlockedIn/scripts/extract_assets.py
python D:/ExtractApk/BlockedIn/scripts/render_levels.py
python D:/ExtractApk/_sound_tools/gen_player.py
```

`extract_assets.py` đọc `extracted/apk/` và ghi lại `images/` (giữ nguyên `images/levels/`), `models/`, `fonts/`, `data/`. `render_levels.py` cần chạy sau script này. `glb.py` là bộ ghi GLB, `render3d.py` là bộ render preview bằng numpy.

## 7. Giới hạn

- **Code**: game build bằng IL2CPP. `libil2cpp.so` nằm trong split ABI nên không có trong APK này; code C# chưa dịch ngược. Tên lớp của các ScriptableObject và MonoBehaviour vẫn đọc được qua type tree của bundle.
- **Màu của một số material là ước lượng.** Game dùng shader riêng (`BlockOrder/*`), nên khi xuất tôi phải nướng sẵn màu. Khối (`PieceBlock`) dùng đúng cách tra ColorRamp như shader nên tin được. Phần ước lượng gồm: màu tường (lấy `_BaseColor` của `BoardBlockMaterial`), dây thừng, khối băng và tay cầm kéo.
- **Khối nhiều ô chưa được dựng lại.** Cách ghép (`Cover_*`, `Tab`/`Socket`, độ dời cạnh trong shader qua màu đỉnh) nằm trong code IL2CPP nên chưa tái hiện được. Hiện chỉ có khối 1×1, đã kiểm tra là ghép khớp.
- **Chỉ có level 1–100.** `RemoteConfig` trỏ tới CDN `gs-prd-web-cdn.tripledotapi.com` để tải chapter set, nên các level sau 100 (nếu có) không nằm trong APK.
