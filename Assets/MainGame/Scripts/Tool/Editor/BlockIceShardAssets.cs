using BlockedIn.Blocks;
using UnityEditor;
using UnityEngine;

namespace BlockedIn.EditorTools
{
    public static class BlockIceShardAssets
    {
        const string Folder = "Assets/MainGame/Prefabs/Ice/";
        public static void Configure(BlockIceVisual visual)
        {
            if (visual.HasShardAssets) return;
            Material material = AssetDatabase.LoadAssetAtPath<Material>(Folder + "IceShards.mat");
            if (material == null)
            {
                material = new Material(AssetDatabase.LoadAssetAtPath<Shader>("Assets/MainGame/Material/IceShard.shader"));
                material.SetTexture("_FrostMap", AssetDatabase.LoadAssetAtPath<Texture2D>(
                    "Assets/MainGame/Prefabs/Ice/FrostSurface.png"));
                AssetDatabase.CreateAsset(material, Folder + "IceShards.mat");
            }
            var meshes = new Mesh[6];
            for (int i = 0; i < meshes.Length; i++)
            {
                string path = Folder + "IceShard_" + i + ".asset";
                meshes[i] = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if (meshes[i] != null) continue;
                meshes[i] = Build(i);
                AssetDatabase.CreateAsset(meshes[i], path);
            }
            visual.ConfigureShardAssets(material, meshes);
        }

        public static Mesh Build(int index)
        {
            float width=.28f+index*.025f, length=.45f-index*.025f;
            Vector3[] points={new Vector3(-width,0,-length),new Vector3(width*.8f,.035f,-length*.6f),
                new Vector3(-width*.3f,.08f,length),new Vector3(-width*.7f,-.12f,-length*.7f),
                new Vector3(width*.65f,-.1f,-length*.4f),new Vector3(-width*.2f,-.07f,length*.7f)};
            int[] faces={0,2,1,3,4,5,0,1,3,1,4,3,1,2,4,2,5,4,2,0,5,0,3,5};
            var vertices=new Vector3[faces.Length];var uv=new Vector2[faces.Length];
            var triangles=new int[faces.Length];
            for(int i=0;i<faces.Length;i++)
            {
                vertices[i]=points[faces[i]];triangles[i]=i;
                uv[i]=new Vector2(vertices[i].x+.5f,vertices[i].z+.5f);
            }
            var mesh=new Mesh{name="IceShardFacet_"+index,vertices=vertices,uv=uv,triangles=triangles};
            mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
    }
}
