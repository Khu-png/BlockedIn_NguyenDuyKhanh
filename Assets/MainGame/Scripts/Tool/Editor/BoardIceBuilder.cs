using System;
using System.Text;
using BlockedIn.Blocks;
using UnityEditor;
using UnityEngine;

namespace BlockedIn.EditorTools
{
    public static class BoardIceBuilder
    {
        const string Folder="Assets/MainGame/Prefabs/Ice";
        public static void Apply(BlockBoard board, BlockIceData[] settings)
        {
            if(board==null) return;
            foreach(Block block in board.Blocks)
            {
                int count=0;
                foreach(BlockIceData entry in settings ?? Array.Empty<BlockIceData>())
                    if(entry.colorId==block.ColorData.ColorId) count=Mathf.Max(0,entry.count);
                if(count==0) continue;
                Build(block,count);
            }
        }
        static void Build(Block block,int count)
        {
            if(block.IceVisual!=null) UnityEngine.Object.DestroyImmediate(block.IceVisual);
            var root=new GameObject("IceVisual"); root.transform.SetParent(block.Visual,false);
            root.transform.localRotation=Quaternion.Inverse(block.Visual.localRotation);
            var shell=new GameObject("IceShell"); shell.transform.SetParent(root.transform,false);
            MeshFilter filter=shell.AddComponent<MeshFilter>(); filter.sharedMesh=LoadMesh(block.Cells);
            MeshRenderer renderer=shell.AddComponent<MeshRenderer>();
            renderer.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/IceShell.mat");
            if(renderer.sharedMaterial==null) throw new InvalidOperationException("Assign the ice shell material at its default path.");
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            var label=new GameObject("CountText"); label.transform.SetParent(root.transform,false);
            label.transform.localPosition=CountPosition(block.Cells);
            label.transform.localRotation=Quaternion.LookRotation(Vector3.down,Vector3.forward);
            MeshRenderer textRenderer=label.AddComponent<MeshRenderer>();
            TextMesh text=label.AddComponent<TextMesh>();
            text.font=AssetDatabase.LoadAssetAtPath<Font>("Assets/MainGame/Resrouces/BlockedIn/BlockedIn/fonts/Roboto-Bold.ttf");
            if(text.font==null) throw new InvalidOperationException("The ice counter font is missing.");
            text.fontSize=96; text.characterSize=.045f; text.anchor=TextAnchor.MiddleCenter; text.color=Color.white;
            textRenderer.sharedMaterial=text.font.material; textRenderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            BlockIceVisual presentation=root.AddComponent<BlockIceVisual>(); presentation.Configure(filter,renderer,text);
            BlockIceShardAssets.Configure(presentation);
            block.ConfigureIce(presentation,count);
        }
        static Mesh LoadMesh(Vector2Int[] cells)
        {
            var key=new StringBuilder(); foreach(Vector2Int cell in cells) key.Append(cell.x).Append(':').Append(cell.y).Append(';');
            string path=Folder+"/IceShape_"+Hash128.Compute(key.ToString())+".asset";
            Mesh mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(mesh==null) { mesh=BlockIceMeshBuilder.Build(cells); AssetDatabase.CreateAsset(mesh,path); }
            if (!mesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.TexCoord3) || mesh.name != "BlockIceShell_Crystal_V2")
            {
                Mesh updated=BlockIceMeshBuilder.Build(cells);
                EditorUtility.CopySerialized(updated,mesh);EditorUtility.SetDirty(mesh);
                UnityEngine.Object.DestroyImmediate(updated);
            }
            return mesh;
        }
        static Vector3 CountPosition(Vector2Int[] cells)
        {
            Vector2 center=Vector2.zero; foreach(Vector2Int cell in cells) center+=cell; center/=cells.Length;
            foreach(Vector2Int cell in cells)
                if(Mathf.Abs(center.x-cell.x)<=.5f && Mathf.Abs(center.y-cell.y)<=.5f) return new Vector3(center.x,.62f,-center.y);
            Vector2 best=cells[0]; float distance=float.PositiveInfinity;
            // Keep the count on the shape even when a concave shape's center lies in a hole.
            foreach(Vector2Int cell in cells)
            {
                Vector2 point=new Vector2(Mathf.Clamp(center.x,cell.x-.3f,cell.x+.3f),Mathf.Clamp(center.y,cell.y-.3f,cell.y+.3f));
                if((point-center).sqrMagnitude>=distance) continue;
                distance=(point-center).sqrMagnitude; best=point;
            }
            return new Vector3(best.x,.62f,-best.y);
        }
    }
}
