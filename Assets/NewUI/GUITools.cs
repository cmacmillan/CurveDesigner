using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.NewUI
{
    public static class GUITools
    {
        public static Rect GetRectCenteredAtPosition(Vector2 position, int halfWidth, int halfHeight)
        {
            return new Rect(position.x - halfWidth, position.y - halfHeight, 2 * halfWidth, 2 * halfHeight);
        }
        public static float CameraDistanceToPoint(Vector3 worldPos)
        {
            WorldToGUISpace(worldPos, out Vector2 guiPosition, out float depth);
            return depth;
        }
        public static bool WorldToGUISpace(Vector3 worldPos, out Vector2 guiPosition, out float screenDepth)
        {
            var sceneCam = UnityEditor.SceneView.lastActiveSceneView.camera;//Consider replacing with Camera.current?
            Vector3 screen_pos = sceneCam.WorldToScreenPoint(worldPos);
            screenDepth = screen_pos.z;
            if (screen_pos.z < 0)
            {
                guiPosition = Vector2.zero;
                return false;
            }
            guiPosition = ScreenSpaceToGuiSpace(screen_pos);
            return true;
        }
        public static Vector3 GUIToWorldSpace(Vector2 guiPos, float screenDepth)
        {
            var sceneCam = UnityEditor.SceneView.lastActiveSceneView.camera;
            Vector3 screen_pos = GuiSpaceToScreenSpace(guiPos);
            screen_pos.z = screenDepth;
            return sceneCam.ScreenToWorldPoint(screen_pos);
        }
        public static Vector2 ScreenSpaceToGuiSpace(Vector2 screenPos)
        {
            return new Vector2(screenPos.x, UnityEditor.SceneView.lastActiveSceneView.camera.pixelHeight - screenPos.y);//Consider replacing with UnityEditor.SceneView.lastActiveSceneView.camera
        }
        public static Vector2 GuiSpaceToScreenSpace(Vector2 guiPos)
        {
            return new Vector2(guiPos.x, UnityEditor.SceneView.lastActiveSceneView.camera.pixelHeight - guiPos.y);
        }
    }
}
