using System;
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

        public PointComposite(IComposite parent, IPositionProvider positionProvider, PointTextureType textureType, IClickCommand clickAction, Color color,SelectableGUID guid) : base(parent)
        {
            this._position = positionProvider;
            this._pointTexture = textureType;
            this._clickAction = clickAction;
            this._color = color;
            this.guid = guid;
        }

        public override SelectableGUID Guid => guid;

        public override void Click(Vector2 mousePosition, List<ClickHitData> clickHits,EventType eventType)
        {
            GUITools.WorldToGUISpace(_position.Position,out Vector2 guiPosition,out float screenDepth);
            float distance = Vector2.Distance(mousePosition,guiPosition);
            clickHits.Add(new ClickHitData(this,distance,screenDepth,guiPosition-mousePosition));
        }

        public override void Draw(List<IDraw> drawList,ClickHitData closestElementToCursor)
        {
            drawList.Add(new PointDraw(this,_position.Position, _pointTexture,_color));
        }

        public override IClickCommand GetClickCommand()
        {
            return _clickAction;
        }
    }
}
