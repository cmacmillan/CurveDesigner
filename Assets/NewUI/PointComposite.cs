using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.NewUI
{
    public class PointComposite : IComposite
    {
        private IPositionProvider _position;
        private PointTextureType _pointTexture;
        private IClickCommand _clickAction;
        
        public PointComposite(IPositionProvider positionProvider,PointTextureType textureType,IClickCommand clickAction)
        {
            this._position = positionProvider;
            this._pointTexture = textureType;
            this._clickAction = clickAction;
        }

        public override void Click(Vector2 position, List<ClickHitData> clickHits)
        {
            GUITools.WorldToGUISpace(_position.Position,out Vector2 guiPosition,out float screenDepth);
            float distance = Vector2.Distance(position,guiPosition);
            clickHits.Add(new ClickHitData(this,distance,screenDepth,_clickAction));
        }

        public override void Draw(List<IDraw> drawList)
        {
            drawList.Add(new PointDraw(_position.Position, _pointTexture));
        }
    }
}
