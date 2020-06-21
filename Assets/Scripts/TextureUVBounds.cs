using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.NewUI
{
    public class TextureUVBounds
    {
        public Vector2 yMinMax;
        public float xScale;
        public static void PopulateUVBounds(Curve3D curve, out TextureUVBounds interior, out TextureUVBounds exterior, out TextureUVBounds edge)
        {
            interior = null;
            exterior = null;
            edge = null;
            var type = curve.type;
            bool hasEdge = type != CurveType.Cylinder;//only cylinders don't have edges
            bool shouldInnerOuterUseSeperateTextures = curve.useSeperateInnerAndOuterFaceTextures;
            int numTexsToPack = 1 + (hasEdge ? 1 : 0) + (shouldInnerOuterUseSeperateTextures ? 1 : 0);
            float textureStepSize = 1.0f / numTexsToPack;
            float minOffset = 0;
            if (hasEdge)
                edge = new TextureUVBounds() { xScale = curve.edgeTextureScale, yMinMax = new Vector2(minOffset, minOffset += textureStepSize) };
            exterior = new TextureUVBounds() { xScale = curve.outerFaceTextureScale, yMinMax = new Vector2(minOffset, minOffset += textureStepSize) };
            if (shouldInnerOuterUseSeperateTextures)
                interior = new TextureUVBounds() { xScale = curve.innerFaceTextureScale, yMinMax = new Vector2(minOffset, minOffset += textureStepSize) };
            else
                interior = exterior;
        }
    }
}
