using System.Collections.Generic;
using UnityEngine;

namespace BlockedIn.EditorTools
{
    public static class BlockIceMeshBuilder
    {
        public static Mesh Build(Vector2Int[] cells, int[] tabs = null)
        {
            var occupied = new HashSet<Vector2Int>(cells);
            var vertices = new List<Vector3>();var triangles = new List<int>();
            var uv = new List<Vector2>();var local = new List<Vector2>();
            var borders = new List<Vector4>();var surfaces = new List<Vector2>();
            Vector2 min = cells[0], max = cells[0];
            foreach (var cell in cells) { min = Vector2.Min(min,cell);max = Vector2.Max(max,cell); }
            Vector2 size = max-min+Vector2.one;
            for (int cellIndex = 0; cellIndex < cells.Length; cellIndex++)
            {
                Vector2Int cell = cells[cellIndex];
                bool left=!occupied.Contains(cell+Vector2Int.left),right=!occupied.Contains(cell+Vector2Int.right);
                bool top=!occupied.Contains(cell+Vector2Int.down),bottom=!occupied.Contains(cell+Vector2Int.up);
                Vector4 flags=new Vector4(left?1:0,right?1:0,top?1:0,bottom?1:0);
                Vector2 center=new Vector2(cell.x,-cell.y);
                int exposedMask=(top?1:0)|(right?2:0)|(bottom?4:0)|(left?8:0);
                int ports=tabs != null && cellIndex < tabs.Length ? tabs[cellIndex] : 0;
                Vector2[] inner=BlockIceContour.Outline(center,new[]{top?.41f:.5f,right?.41f:.5f,bottom?.41f:.5f,left?.41f:.5f},.08f,exposedMask,ports,false);
                Vector2[] outer=BlockIceContour.Outline(center,new[]{top?.54f:.5f,right?.54f:.5f,bottom?.54f:.5f,left?.54f:.5f},.16f,exposedMask,ports,true);
                int first=vertices.Count;
                foreach(var point in inner)Add(point,.58f,0,0);
                foreach(int index in BlockIceContour.Triangulate(inner)) triangles.Add(first+index);
                for(int i=0;i<inner.Length;i++)
                {
                    int next=(i+1)%inner.Length;
                    Vector2 mid=(outer[i]+outer[next])*.5f-center;
                    bool exposed=Mathf.Abs(mid.x)>Mathf.Abs(mid.y)?(mid.x<0?left:right):(mid.y>0?top:bottom);
                    if(!exposed || (outer[i]-outer[next]).sqrMagnitude<.000001f)continue;
                    Vector2 a=outer[i],b=outer[next];
                    Quad(inner[i],.58f,inner[next],.58f,a,.47f,b,.47f,1);
                    Quad(a,.47f,b,.47f,a,.29f,b,.29f,2);
                }
                int Add(Vector2 point,float height,float layer,float depth)
                {
                    int index=vertices.Count;vertices.Add(new Vector3(point.x,height,point.y));
                    uv.Add(new Vector2((point.x-min.x+.5f)/size.x,(-point.y-min.y+.5f)/size.y));
                    local.Add(new Vector2(point.x-cell.x+.5f,-point.y-cell.y+.5f));
                    borders.Add(flags);surfaces.Add(new Vector2(layer,depth));return index;
                }
                void Quad(Vector2 a,float ay,Vector2 b,float by,Vector2 c,float cy,Vector2 d,float dy,float layer)
                {
                    int start=vertices.Count;
                    Add(a,ay,layer,0);Add(b,by,layer,0);Add(c,cy,layer,1);Add(d,dy,layer,1);
                    triangles.AddRange(new[]{start,start+2,start+1,start+1,start+2,start+3});
                }

            }
            var mesh=new Mesh{name="BlockIceShell_Tabbed_V4"};
            mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);
            mesh.SetUVs(0,uv);mesh.SetUVs(1,local);mesh.SetUVs(2,borders);mesh.SetUVs(3,surfaces);
            mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }

    }
}
