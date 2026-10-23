using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace BlockedIn.CameraTools
{
    public sealed partial class CameraManager
    {
        [SerializeField] UniversalAdditionalCameraData iceCameraData;
        static readonly int IceSceneColor = Shader.PropertyToID("_IceSceneColorAvailable");
        bool iceCopyEnabled, iceCameraCaptured;
        CameraOverrideOption originalColorOption;

        void UpdateIceRefraction()
        {
            bool visible = false;
            if (board != null && board.blockGameplay != null)
                foreach (var block in board.blockGameplay.Blocks)
                    if (block != null && block.IceVisual != null && block.IceVisual.activeInHierarchy)
                    { visible = true; break; }
            if (iceCameraData == null) { Shader.SetGlobalFloat(IceSceneColor, 0); return; }
            if (!iceCameraCaptured)
            {
                originalColorOption = iceCameraData.requiresColorOption;
                iceCameraCaptured = true;
            }
            if (iceCopyEnabled == visible) return;
            iceCopyEnabled = visible;
            iceCameraData.requiresColorOption = visible ? CameraOverrideOption.On : originalColorOption;
            Shader.SetGlobalFloat(IceSceneColor, visible ? 1 : 0);
        }
        void OnDisable()
        {
            if (iceCameraData != null && iceCameraCaptured) iceCameraData.requiresColorOption = originalColorOption;
            iceCopyEnabled = false;
            Shader.SetGlobalFloat(IceSceneColor, 0);
        }
    }
}
