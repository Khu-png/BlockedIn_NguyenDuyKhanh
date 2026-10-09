using System;
using BlockedIn.Blocks;
using BlockedIn.Blocks.Editor;
using UnityEditor;
using UnityEngine;

namespace BlockedIn.EditorTools
{
    public sealed class BlockColorPalette : PopupWindowContent
    {
        BlockColorData selected;
        BlockEdgeData edges;
        readonly bool[] internalEdges;
        readonly Action<BlockColorData> choose;
        readonly Action<BlockEdgeData> changeEdges;
        static readonly string[] edgeNames = { "Default", "Tab", "Socket" };
        readonly string[] names = { "Black", "Blue", "ForestGreen", "Green", "Obstacle", "Orange",
            "Pink", "Purple", "Red", "Turquoise", "White", "Yellow" };

        public BlockColorPalette(BlockColorData current, BlockEdgeData currentEdges,
            Action<BlockColorData> onChoose, Action<BlockEdgeData> onChangeEdges, bool[] internalSides = null)
        {
            internalEdges = internalSides;
            selected = current;
            choose = onChoose;
            edges = currentEdges;
            changeEdges = onChangeEdges;
        }

        public override Vector2 GetWindowSize() => new Vector2(510, 235);

        public override void OnGUI(Rect rect)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(290))) DrawColors();
                GUILayout.Space(10);
                using (new EditorGUILayout.VerticalScope()) DrawEdges();
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Remove Block")) Select(null);
                if (GUILayout.Button("Done")) CloseLater();
            }
        }

        void DrawColors()
        {
            EditorGUILayout.LabelField("Block Color", EditorStyles.boldLabel);
            for (int row = 0; row < 4; row++)
                using (new EditorGUILayout.HorizontalScope())
                    for (int column = 0; column < 3; column++)
                        DrawColor(names[row * 3 + column]);
            EditorGUILayout.Space();
        }

        void DrawEdges()
        {
            EditorGUILayout.LabelField("Block Edges", EditorStyles.boldLabel);
            for (int i = 0; i < 4; i++)
            {
                BlockSide side = (BlockSide)i;
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(side.ToString(), GUILayout.Width(52));
                    if (internalEdges != null && internalEdges[i])
                    { GUILayout.Label("Internal (Default)"); continue; }
                    EditorGUI.BeginChangeCheck();
                    int index = Mathf.Clamp((int)edges.Get(side), 0, edgeNames.Length - 1);
                    EdgeType type = (EdgeType)EditorGUILayout.Popup(index, edgeNames);
                    if (EditorGUI.EndChangeCheck()) SetEdge(side, type);
                }
            }
            EditorGUILayout.HelpBox("Top points toward +Z. Color applies to the entire shape; edges apply to the clicked cell. Internal edges stay visible as Default. Generate applies changes.", MessageType.None);
        }

        void SetEdge(BlockSide side, EdgeType type)
        {
            edges.Set(side, type);
            changeEdges(edges);
        }

        void DrawColor(string name)
        {
            BlockColorData color = BlockAssetBuilder.LoadColor(name);
            Color previous = GUI.backgroundColor;
            GUI.backgroundColor = Tint(color);
            using (new EditorGUI.DisabledScope(color == null || color.Material == null))
                if (GUILayout.Button((color == selected ? "✓ " : "") + name, GUILayout.Height(34))) Select(color);
            GUI.backgroundColor = previous;
        }

        void Select(BlockColorData color)
        {
            choose(color);
            selected = color;
            if (color == null) CloseLater();
        }

        void CloseLater()
        {
            EditorWindow window = editorWindow;
            EditorApplication.delayCall += () => { if (window != null) window.Close(); };
        }

        public static Color Tint(BlockColorData color)
        {
            if (color == null) return Color.gray;
            string name = color.name.Replace("BlockColor_", "");
            switch (name)
            {
                case "Black": return new Color(.15f, .15f, .17f);
                case "Blue": return new Color(.2f, .55f, .95f);
                case "ForestGreen": return new Color(.12f, .38f, .24f);
                case "Green": return new Color(.35f, .8f, .35f);
                case "Obstacle": return new Color(.45f, .53f, .54f);
                case "Orange": return new Color(1f, .55f, .15f);
                case "Pink": return new Color(1f, .5f, .7f);
                case "Purple": return new Color(.65f, .35f, .85f);
                case "Red": return new Color(.95f, .25f, .25f);
                case "Turquoise": return new Color(.2f, .8f, .8f);
                case "White": return new Color(.9f, .92f, .95f);
                case "Yellow": return new Color(1f, .85f, .25f);
                default: return color.Material != null ? color.Material.color : Color.gray;
            }
        }
    }
}
