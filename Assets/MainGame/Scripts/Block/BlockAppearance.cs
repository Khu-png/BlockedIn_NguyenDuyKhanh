using System;
using System.Collections.Generic;
using UnityEngine;

namespace BlockedIn.Blocks
{
    public enum BlockSide { Top, Right, Bottom, Left }
    public enum EdgeType { Default, Tab, Socket, None }
    public enum CornerType { Default, CornerToStraight, None }

    [Serializable]
    public sealed class BlockEdgeParts
    {
        public EdgeType type;
        public GameObject defaultEdge;
        public GameObject tab;
        public GameObject socket;

        public void Apply()
        {
            if (defaultEdge != null) defaultEdge.SetActive(type == EdgeType.Default);
            if (tab != null) tab.SetActive(type == EdgeType.Tab);
            if (socket != null) socket.SetActive(type == EdgeType.Socket);
        }
    }

    [Serializable]
    public sealed class BlockCornerParts
    {
        public CornerType type;
        public GameObject defaultCorner;
        public GameObject cornerToStraight;

        public void Apply()
        {
            if (defaultCorner != null) defaultCorner.SetActive(type == CornerType.Default);
            if (cornerToStraight != null) cornerToStraight.SetActive(type == CornerType.CornerToStraight);
        }
    }

    public sealed class BlockAppearance : MonoBehaviour
    {
        [Tooltip("Top, Right, Bottom, Left")]
        [SerializeField] BlockEdgeParts[] edges = new BlockEdgeParts[4];
        [Tooltip("Top Left, Top Right, Bottom Right, Bottom Left")]
        [SerializeField] BlockCornerParts[] corners = new BlockCornerParts[4];

        public EdgeType GetEdge(BlockSide side) => edges[(int)side].type;
        public CornerType GetCorner(int index) => corners[index].type;

        public void SetEdge(BlockSide side, EdgeType type)
        {
            edges[(int)side].type = type;
            edges[(int)side].Apply();
        }

        public void SetCorner(int index, CornerType type)
        {
            corners[index].type = type;
            corners[index].Apply();
        }

        public void Apply()
        {
            foreach (BlockEdgeParts edge in edges) edge?.Apply();
            foreach (BlockCornerParts corner in corners) corner?.Apply();
        }

        public IEnumerable<GameObject> Parts()
        {
            foreach (BlockEdgeParts edge in edges)
            {
                if (edge == null) continue;
                yield return edge.defaultEdge;
                yield return edge.tab;
                yield return edge.socket;
            }
            foreach (BlockCornerParts corner in corners)
            {
                if (corner == null) continue;
                yield return corner.defaultCorner;
                yield return corner.cornerToStraight;
            }
        }

        void OnValidate() => Apply();
    }
}
