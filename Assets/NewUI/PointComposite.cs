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
        private IPositionProvider position;
        private PointTextureType pointTexture;
        private IClickCommand clickAction;
        
        public PointComposite(IPositionProvider positionProvider,PointTextureType textureType,IClickCommand clickAction)
        {
            this.position = positionProvider;
            this.pointTexture = textureType;
            this.clickAction = clickAction;
        }

        public override ClickHitData Click(Vector2 mousePos)
        {
            throw new NotImplementedException();
        }

        public override void Draw(List<IDraw> drawList)
        {
            drawList.Add(new PointDraw(position.Position, pointTexture));
        }

        public override IEnumerable<IComposite> GetChildren()
        {
            return Enumerable.Empty<IComposite>();
        }
    }
}
