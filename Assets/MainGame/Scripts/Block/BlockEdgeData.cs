using System;

namespace BlockedIn.Blocks
{
    [Serializable]
    public struct BlockEdgeData
    {
        public EdgeType top;
        public EdgeType right;
        public EdgeType bottom;
        public EdgeType left;

        public EdgeType Get(BlockSide side)
        {
            EdgeType value;
            switch (side)
            {
                case BlockSide.Top: value = top; break;
                case BlockSide.Right: value = right; break;
                case BlockSide.Bottom: value = bottom; break;
                default: value = left; break;
            }
            return value == EdgeType.None ? EdgeType.Default : value;
        }

        public void Normalize()
        {
            for (int i = 0; i < 4; i++) Set((BlockSide)i, Get((BlockSide)i));
        }

        public void Set(BlockSide side, EdgeType type)
        {
            switch (side)
            {
                case BlockSide.Top: top = type; break;
                case BlockSide.Right: right = type; break;
                case BlockSide.Bottom: bottom = type; break;
                case BlockSide.Left: left = type; break;
            }
        }

        public bool IsValid => Valid(top) && Valid(right) && Valid(bottom) && Valid(left);
        static bool Valid(EdgeType type) => type >= EdgeType.Default && type <= EdgeType.None;

        public void Apply(BlockAppearance appearance)
        {
            if (appearance == null) throw new InvalidOperationException("Assign the block appearance reference.");
            for (int i = 0; i < 4; i++) appearance.SetEdge((BlockSide)i, Get((BlockSide)i));
        }
    }
}
