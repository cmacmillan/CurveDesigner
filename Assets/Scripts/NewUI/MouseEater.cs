using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.NewUI
{
    public static class MouseEater
    {
        private static readonly int _CurveHint = "NewGUI.CURVE".GetHashCode();//duplicated code
        public static void EatMouseInput(Rect position)
        {
            int id = GUIUtility.GetControlID(_CurveHint, FocusType.Passive, position);
            var type = Event.current.GetTypeForControl(id);
            switch (type)
            {
                case EventType.MouseDown:
                    if (position.Contains(Event.current.mousePosition))
                    {
                        GUIUtility.hotControl = id;
                        Event.current.Use();
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id)
                    {
                        GUIUtility.hotControl = 0;
                        Event.current.Use();
                    }
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id)
                        Event.current.Use();
                    break;
                case EventType.ScrollWheel:
                    if (position.Contains(Event.current.mousePosition))
                        Event.current.Use();
                    break;
            }
        }
    }
}
