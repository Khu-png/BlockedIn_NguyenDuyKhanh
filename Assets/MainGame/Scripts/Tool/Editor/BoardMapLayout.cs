using UnityEditor;
using UnityEngine;

namespace BlockedIn.EditorTools
{
    public partial class BoardMapWindow
    {
        [SerializeField] float cellSize = 42f;
        Vector2 settingsScroll;
        bool showWorkflowHelp;
        GUIStyle gridLabel;
        const float SidebarWidth = 330f;
        bool WideLayout => position.width >= 820f;

        void DrawWorkspace()
        {
            DrawWorkspaceHeader();
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                DrawWorkflowTabs();
                if (WideLayout)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        using (new EditorGUILayout.VerticalScope(GUILayout.Width(SidebarWidth)))
                            DrawInspectorPane(false);
                        GUILayout.Space(6);
                        DrawPreviewPane();
                    }
                }
                else
                {
                    DrawInspectorPane(true);
                    DrawPreviewPane();
                }
                DrawSaveFooter();
            }
        }
        void DrawWorkspaceHeader()
        {
            GUILayout.Space(8);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(10);
                using (new EditorGUILayout.VerticalScope())
                {
                    GUILayout.Label(WindowTitle, EditorStyles.largeLabel);
                    GUILayout.Label("BLOCKED IN  /  LEVEL DESIGN", EditorStyles.miniLabel);
                }
                GUILayout.FlexibleSpace();
                GUILayout.Label("Level " + levelNumber + "   |   " + columns + " x " + rows,
                    EditorStyles.boldLabel, GUILayout.Width(145));
            }
            GUILayout.Space(8);
        }
        void DrawWorkflowTabs()
        {
            if (!UsesAssemblyWorkflow) return;
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                int next = GUILayout.Toolbar(assemblyStage,
                    new[] { "1. Build Shape", "2. Cut Pieces", "3. Arrange" }, EditorStyles.toolbarButton);
                if (next != assemblyStage) Run(() => SetAssemblyStage(next));
            }
            GUILayout.Space(6);
        }
        void DrawInspectorPane(bool compact)
        {
            using (var view = compact
                ? new EditorGUILayout.ScrollViewScope(settingsScroll, GUILayout.Height(240))
                : new EditorGUILayout.ScrollViewScope(settingsScroll, GUILayout.ExpandHeight(true)))
            {
                settingsScroll = view.scrollPosition;
                float previousWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 128;
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    SectionTitle("LEVEL", "Choose a map or set up a new level.");
                    DrawStorage();
                }
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    SectionTitle("BOARD", "Playable grid dimensions");
                    DrawSettings();
                }
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    SectionTitle("EDITING", UsesAssemblyWorkflow ? "Tools for the current step" : "Paint cells or edit groups");
                    DrawShapeTools();
                    GUILayout.Space(4);
                    if (UsesAssemblyWorkflow) DrawAssemblyBrushes();
                    else DrawClassicBrushes();
                }
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox)) DrawPrefabSettings();
                DrawWorkflowHelp();
                EditorGUIUtility.labelWidth = previousWidth;
            }
        }
        static void SectionTitle(string title, string detail)
        {
            GUILayout.Space(3);
            GUILayout.Label(title, EditorStyles.boldLabel);
            GUILayout.Label(detail, EditorStyles.wordWrappedMiniLabel);
            GUILayout.Space(5);
        }
        void DrawWorkflowHelp()
        {
            showWorkflowHelp = EditorGUILayout.Foldout(showWorkflowHelp, "Workflow help", true);
            if (!showWorkflowHelp) return;
            EditorGUILayout.HelpBox(UsesAssemblyWorkflow
                ? "Build a complete shape, cut pieces, then drag them into a puzzle. Right-click a block to edit its color, ports, Ice or Rope."
                : "Paint blocks, then click one to edit its color and ports. Edit Shape selects cells for merging or splitting.", MessageType.Info);
            GUILayout.Label("Saving builds the level prefab and updates LevelManager.", EditorStyles.wordWrappedMiniLabel);
        }
        void DrawPreviewPane()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.ExpandHeight(true)))
            {
                using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
                {
                    GUILayout.Label("MAP PREVIEW", EditorStyles.miniBoldLabel);
                    GUILayout.FlexibleSpace();
                    float zoom = GUILayout.HorizontalSlider(cellSize, 20, 72, GUILayout.Width(90));
                    if (!Mathf.Approximately(zoom, cellSize)) SetGridZoom(zoom);
                    GUILayout.Label(Mathf.RoundToInt(cellSize / 42 * 100) + "%", GUILayout.Width(38));
                    if (GUILayout.Button("Fit", EditorStyles.toolbarButton, GUILayout.Width(32))) FitGrid();
                }
                DrawCenteredPreview();
                GUILayout.Label("B  Block     G  Group     W  Wall     I  Ice     R  Rope", EditorStyles.miniLabel);
                string hint = UsesAssemblyWorkflow && assemblyStage == 2
                    ? "Drag to move or swap / right-click to edit"
                    : "Click blocks to edit / drag to paint or select";
                GUILayout.Label("Selected cells: " + selectedCells.Count + "   |   " + hint,
                    EditorStyles.wordWrappedMiniLabel);
            }
        }
        void DrawCenteredPreview()
        {
            Rect viewport = GUILayoutUtility.GetRect(0, 100000, 0, 100000,
                GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            Vector2 mapSize = new Vector2(columns * cellSize, rows * cellSize);
            Vector2 available = viewport.size;
            float verticalBar = GUI.skin.verticalScrollbar.fixedWidth + GUI.skin.verticalScrollbar.margin.left;
            float horizontalBar = GUI.skin.horizontalScrollbar.fixedHeight + GUI.skin.horizontalScrollbar.margin.top;
            for (int i = 0; i < 3; i++)
                available = new Vector2(
                    viewport.width - (mapSize.y + 32 > available.y ? verticalBar : 0),
                    viewport.height - (mapSize.x + 32 > available.x ? horizontalBar : 0));
            Vector2 content = Vector2.Max(mapSize + Vector2.one * 32, available);
            scroll = GUI.BeginScrollView(viewport, scroll, new Rect(Vector2.zero, content));
            DrawGrid(new Rect((content - mapSize) * .5f, mapSize));
            GUI.EndScrollView();
        }
        void FitGrid()
        {
            float width = position.width - (WideLayout ? SidebarWidth + 48 : 40);
            float height = position.height - (WideLayout ? 220 : 460);
            SetGridZoom(Mathf.Clamp(Mathf.Min(width / columns, height / rows), 20, 72));
            scroll = Vector2.zero;
        }
        void SetGridZoom(float value)
        {
            CancelAssemblyDrag();
            cellSize = value;
        }
        void DrawSaveFooter()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox, GUILayout.Height(42)))
            {
                GUILayout.Label(mapAsset != null ? mapAsset.name : "New unsaved map",
                    EditorStyles.miniLabel, GUILayout.MinWidth(60));
                GUILayout.FlexibleSpace();
                using (new EditorGUI.DisabledScope(mapAsset == null))
                    if (GUILayout.Button("Save Map", GUILayout.Width(90), GUILayout.Height(28)))
                        Run(() => SaveTo(mapAsset));
                Color previous = GUI.backgroundColor;
                GUI.backgroundColor = new Color(.45f, .78f, 1f);
                if (GUILayout.Button("Save as Level", GUILayout.Width(120), GUILayout.Height(28))) Run(SaveAsLevel);
                GUI.backgroundColor = previous;
            }
        }
    }
}
