using System.Collections.Generic;
using UnityEngine;

namespace BlockedIn.EditorTools
{
    public static class BlockIceMeshBuilder
    {
        public static Mesh Build(Vector2Int[] cells)
        {
            var occupied = new HashSet<Vector2Int>(cells);
            var vertices = new List<Vector3>();var triangles = new List<int>();
            var uv = new List<Vector2>();var local = new List<Vector2>();
            var borders = new List<Vector4>();var surfaces = new List<Vector2>();
            Vector2 min = cells[0], max = cells[0];
            foreach (var cell in cells) { min = Vector2.Min(min,cell);max = Vector2.Max(max,cell); }
            Vector2 size = max-min+Vector2.one;
            foreach (var cell in cells)
            {
                bool left=!occupied.Contains(cell+Vector2Int.left),right=!occupied.Contains(cell+Vector2Int.right);
                bool top=!occupied.Contains(cell+Vector2Int.down),bottom=!occupied.Contains(cell+Vector2Int.up);
                Vector4 flags=new Vector4(left?1:0,right?1:0,top?1:0,bottom?1:0);
                Vector2 center=new Vector2(cell.x,-cell.y);
                Vector2[] inner=Outline(center,left?.41f:.5f,right?.41f:.5f,top?.41f:.5f,bottom?.41f:.5f,.055f,left,right,top,bottom);
                Vector2[] outer=Outline(center,left?.54f:.5f,right?.54f:.5f,top?.54f:.5f,bottom?.54f:.5f,.11f,left,right,top,bottom);
                int middle=Add(center,.58f,0,0);
                int first=vertices.Count;
                foreach(var point in inner)Add(point,.58f,0,0);
                for(int i=0;i<8;i++)
                {
                    int next=(i+1)%8;
                    if ((inner[i]-inner[next]).sqrMagnitude>.000001f)
                        triangles.AddRange(new[]{middle,first+i,first+next});
                    Vector2 mid=(outer[i]+outer[next])*.5f-center;
                    bool exposed=Mathf.Abs(mid.x)>Mathf.Abs(mid.y)?(mid.x<0?left:right):(mid.y>0?top:bottom);
                    if(!exposed || (outer[i]-outer[next]).sqrMagnitude<.000001f)continue;
                    Vector2 a=Chipped(outer[i]),b=Chipped(outer[next]);
                    Quad(inner[i],.58f,inner[next],.58f,a,.47f,b,.47f,1);
                    Quad(a,.47f,b,.47f,a,.29f,b,.29f,2);
                }
                if(left&&top)Cluster(-1,1);
                if(right&&top)Cluster(1,1);
                if(left&&bottom)Cluster(-1,-1);
                if(right&&bottom)Cluster(1,-1);
                void Cluster(float x,float z)
                {
                    float seed=cell.x*7.13f+cell.y*17.37f+x*3.71f+z*11.91f+cells.Length*.73f;
                    int corner=(x>0?1:0)+(z<0?2:0);
                    int count=1+(corner+cells.Length)%3;
                    for(int i=0;i<count;i++)
                    {
                        float variation=Variation(seed+i*3.17f);
                        float radius=Mathf.Lerp(.075f,.16f,variation)*(i==0?1:.8f);
                        float along=(i-(count-1)*.5f)*.11f;
                        Vector2 point=center+new Vector2(x*(.34f+along),z*(.34f-along));
                        float height=Mathf.Lerp(.6f,.74f,Variation(seed+i*5.43f+2));
                        float angle=Variation(seed+i*7.19f+4)*Mathf.PI;
                        Crystal(point,radius,height,angle,Mathf.Lerp(.65f,1.3f,variation),x,z);
                    }
                }
                void Crystal(Vector2 point,float radius,float height,float angle,float stretch,float x,float z)
                {
                    Vector2 axis=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
                    Vector2 cross=new Vector2(-axis.y,axis.x);
                    Vector2[] foot={point-axis*radius,point+cross*radius*stretch,
                        point+axis*radius,point-cross*radius*stretch};
                    Vector2 tip=point+axis*radius*.3f+new Vector2(x,z)*radius*.1f;
                    for(int face=0;face<4;face++)
                    {
                        int start=vertices.Count;
                        Add(foot[face],.49f,3,face*.25f);Add(foot[(face+1)%4],.49f,3,face*.25f);
                        Add(tip,height,3,face*.25f);triangles.AddRange(new[]{start,start+1,start+2});
                    }
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
                Vector2 Chipped(Vector2 point)
                {
                    float noise=(Mathf.Sin(point.x*19.31f+point.y*27.17f)*437.2f)%1;
                    noise=Mathf.Abs(noise)*.018f;
                    if(left&&point.x<center.x-.49f)point.x-=noise;
                    if(right&&point.x>center.x+.49f)point.x+=noise;
                    if(top&&point.y>center.y+.49f)point.y+=noise;
                    if(bottom&&point.y<center.y-.49f)point.y-=noise;
                    return point;
                }
            }
            var mesh=new Mesh{name="BlockIceShell_Crystal_V2"};
            mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);
            mesh.SetUVs(0,uv);mesh.SetUVs(1,local);mesh.SetUVs(2,borders);mesh.SetUVs(3,surfaces);
            mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }

        static float Variation(float seed)
        {
            return Mathf.Repeat(Mathf.Sin(seed*12.9898f+78.233f)*43758.5453f,1);
        }

        static Vector2[] Outline(Vector2 center,float left,float right,float top,float bottom,float cut,
            bool hasLeft,bool hasRight,bool hasTop,bool hasBottom)
        {
            float tr=hasTop&&hasRight?cut:0,tl=hasTop&&hasLeft?cut:0;
            float bl=hasBottom&&hasLeft?cut:0,br=hasBottom&&hasRight?cut:0;
            return new[]{center+new Vector2(right-tr,top),center+new Vector2(right,top-tr),
                center+new Vector2(right,-bottom+br),center+new Vector2(right-br,-bottom),
                center+new Vector2(-left+bl,-bottom),center+new Vector2(-left,-bottom+bl),
                center+new Vector2(-left,top-tl),center+new Vector2(-left+tl,top)};
        }
    }
}
