using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.NewUI
{
    public delegate void DrawCollapsableCategory(Curve3D curve);
    public static class DrawCollapsableCategoryFunctions
    {
        public static void DrawMainCategory(Curve3D curve)
        {
            GUILayout.Label("we in there (yeet)");
        }
    }
}
