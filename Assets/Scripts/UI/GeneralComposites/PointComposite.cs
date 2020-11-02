﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.NewUI
{
    public class PointComposite : IClickable
    {
        private IPositionProvider _position;
        private PointTextureType _pointTexture;
        private IClickCommand _clickAction;
        private Color _color;
        private SelectableGUID guid;
        private int size;
        private bool hideIfNotHovered;

        public PointComposite(IComposite parent, IPositionProvider positionProvider, PointTextureType textureType, IClickCommand clickAction, Color color, SelectableGUID guid, bool hideIfNotHovered = false,int size=5) : base(parent)
        {
            this._position = positionProvider;
            this._pointTexture = textureType;
            this._clickAction = clickAction;
            this._color = color;
            this.size = size;
            this.guid = guid;
            this.hideIfNotHovered = hideIfNotHovered;
        }

        public override SelectableGUID GUID => guid;
        public override float DistanceFromMouse(Vector2 mouse)
        {
            if (GUITools.WorldToGUISpace(_position.Position, out Vector2 guiPosition, out float screenDepth))
                return Vector2.Distance(mouse, guiPosition);
            return float.MaxValue;
        }

        public override void Click(Vector2 mousePosition, List<ClickHitData> clickHits)
        {
            GUITools.WorldToGUISpace(_position.Position,out Vector2 guiPosition,out float screenDepth);
            clickHits.Add(new ClickHitData(this,screenDepth,guiPosition-mousePosition));
        }

        public override void Draw(List<IDraw> drawList,ClickHitData closestElementToCursor)
        {
            drawList.Add(new PointDraw(this,_position.Position, _pointTexture,_color,size,hideIfNotHovered));
        }

        public override IClickCommand GetClickCommand()
        {
            return _clickAction;
        }
    }
}