using System.Collections.Generic;
using UnityEngine;

namespace BlockedIn.EditorTools
{
    public static class BlockIceMeshBuilder
    {
        public static Mesh Build(Vector2Int[] cells)
        {
            var occupied = new HashSet<Vector2Int>(cells);
            var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var triangles = new List<int>();
            Vector2 min = cells[0], max = cells[0];
            foreach (Vector2Int cell in cells) { min=Vector2.Min(min,cell); max=Vector2.Max(max,cell); }
            Vector2 size=max-min+Vector2.one;
            foreach (Vector2Int cell in cells)
            {
                var outline=new List<Vector2>();
                Vector2Int[] corners={Vector2Int.one,new Vector2Int(-1,1),-Vector2Int.one,new Vector2Int(1,-1)};
                for(int corner=0;corner<4;corner++)
                {
                    Vector2Int sign=corners[corner];
                    bool rounded=!occupied.Contains(cell+new Vector2Int(sign.x,0)) && !occupied.Contains(cell+new Vector2Int(0,sign.y));
                    float radius=rounded ? .12f : 0;
                    Vector2 center=new Vector2(cell.x+sign.x*(.5f-radius),-cell.y-sign.y*(.5f-radius));
                    for(int step=0;step<=4;step++)
                    {
                        // Traverse each convex corner clockwise in the XZ plane.
                        float start=corner==0 ? 0 : corner==1 ? 90 : corner==2 ? 180 : 270;
                        float angle=(start+step*22.5f)*Mathf.Deg2Rad;
                        Vector2 point=center+new Vector2(Mathf.Cos(angle),-Mathf.Sin(angle))*radius;
                        outline.Add(point);
                    }
                }
                int middle=vertices.Count; Add(new Vector3(cell.x,.58f,-cell.y));
                for(int i=0;i<outline.Count;i++) Add(new Vector3(outline[i].x,.58f,outline[i].y));
                for(int i=0;i<outline.Count;i++)
                {
                    int a=middle+1+i,b=middle+1+(i+1)%outline.Count;
                    triangles.AddRange(new[]{middle,b,a});
                    Vector2 edgeCenter=(outline[i]+outline[(i+1)%outline.Count])*.5f;
                    Vector2Int neighbor=cell;
                    Vector2 relative=edgeCenter-new Vector2(cell.x,-cell.y);
                    if(Mathf.Abs(relative.x)>Mathf.Abs(relative.y)) neighbor.x+=relative.x>0 ? 1 : -1;
                    else neighbor.y+=relative.y>0 ? -1 : 1;
                    if(occupied.Contains(neighbor)) continue;
                    int lower=vertices.Count;
                    Add(new Vector3(outline[i].x,.33f,outline[i].y)); Add(new Vector3(outline[(i+1)%outline.Count].x,.33f,outline[(i+1)%outline.Count].y));
                    triangles.AddRange(new[]{a,b,lower,b,lower+1,lower});
                }
            }
            var mesh=new Mesh { name="BlockIceShell" };
            mesh.SetVertices(vertices); mesh.SetUVs(0,uv); mesh.SetTriangles(triangles,0); mesh.RecalculateNormals(); return mesh;
            void Add(Vector3 point)
            {
                vertices.Add(point); uv.Add(new Vector2((point.x-min.x+.5f)/size.x,(-point.z-min.y+.5f)/size.y));
            }
        }
    }
}
