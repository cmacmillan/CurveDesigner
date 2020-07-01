using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.NewUI
{
    //Abstract classes can't be serialized
    public abstract class CollapsableCategory
    {
        public bool isExpanded;
        public abstract string name { get; }
        public abstract void Draw(Curve3D curve);
    }
    public class MainCollapsableCategory : CollapsableCategory
    {
        public override string name => "Main Category";

        public override void Draw(Curve3D curve)
        {
            GUILayout.Label("we in there (yeet)");
        }
    }
    public class TexturesCollapsableCategory : CollapsableCategory
    {
        public override string name => "Textures";

        public override void Draw(Curve3D curve)
        {
            GUILayout.Label("textures");
        }
    }
}
