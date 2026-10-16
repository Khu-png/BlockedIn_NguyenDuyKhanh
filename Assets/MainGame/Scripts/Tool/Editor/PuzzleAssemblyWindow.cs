using UnityEditor;

namespace BlockedIn.EditorTools
{
    public sealed class PuzzleAssemblyWindow : BoardMapWindow
    {
        protected override bool UsesAssemblyWorkflow => true;
        protected override string WindowTitle => "Puzzle Assembly";

        [MenuItem("Tools/Blocked In/Puzzle Assembly")]
        public static void OpenAssembly() => GetWindow<PuzzleAssemblyWindow>("Puzzle Assembly");
    }
}
