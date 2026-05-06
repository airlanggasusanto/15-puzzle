using UnityEngine;
using UnityEngine.UIElements;
using System;

namespace MyGame.UI
{
    [UxmlElement]
    public partial class GameButton : VisualElement
    {
        public event Action OnClick;
        
        private const string BaseClass = "custom-button";
        private const string HoverClass = "custom-button--hover";
        private const string ActiveClass = "custom-button--active";

        public GameButton()
        {
            pickingMode = PickingMode.Position;
            focusable = true;

            AddToClassList(BaseClass);

            RegisterCallback<PointerEnterEvent>(OnPointerEnter);
            RegisterCallback<PointerLeaveEvent>(OnPointerLeave);
            RegisterCallback<PointerMoveEvent>(OnPointerMove);

            RegisterCallback<PointerDownEvent>(OnPointerDown);
            RegisterCallback<PointerUpEvent>(OnPointerUp);

            RegisterCallback<ClickEvent>(OnElementClicked);

            RegisterCallback<AttachToPanelEvent>(_ =>
            {
                SetChildrenPickingMode(this);
            });

            RegisterCallback<PointerOverEvent>(OnPointerOver);
            RegisterCallback<PointerOutEvent>(OnPointerOut);
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (worldBound.Contains(evt.position))
                AddToClassList(HoverClass);
            else
                RemoveFromClassList(HoverClass);
        }

        private void SetChildrenPickingMode(VisualElement element)
        {
            foreach (var child in element.Children())
            {
                child.pickingMode = PickingMode.Ignore;
                SetChildrenPickingMode(child); 
            }
        }

        private void OnPointerEnter(PointerEnterEvent evt)
        {
            AddToClassList(HoverClass);
        }

        private void OnPointerLeave(PointerLeaveEvent evt)
        {
            RemoveFromClassList(HoverClass);
            RemoveFromClassList(ActiveClass);
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            AddToClassList(ActiveClass);
            this.CapturePointer(evt.pointerId);
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            RemoveFromClassList(ActiveClass);
            this.ReleasePointer(evt.pointerId);
        }


        private void OnPointerOver(PointerOverEvent evt)
        {
            AddToClassList(HoverClass);
        }

        private void OnPointerOut(PointerOutEvent evt)
        {
            RemoveFromClassList(HoverClass);
            RemoveFromClassList(ActiveClass);
        }   
        
        private void OnElementClicked(ClickEvent evt)
        {
            OnClick?.Invoke();
        }
    }

    [UxmlElement]
    public partial class WaveLines : VisualElement
    {
        static readonly CustomStyleProperty<float> s_Thickness = new CustomStyleProperty<float>("--line-thickness");
        static readonly CustomStyleProperty<float> s_Amplitude = new CustomStyleProperty<float>("--wave-height");
        static readonly CustomStyleProperty<float> s_Freq = new CustomStyleProperty<float>("--wave-frequency");

        private float _thickness = 1f;
        private float _amplitude = 1f;
        private Color _lineColor = Color.white;
        
        public WaveLines()
        {
            generateVisualContent += DrawWaves;

            RegisterCallback<CustomStyleResolvedEvent>(evt => {
                if (evt.customStyle.TryGetValue(s_Thickness, out float thickness)) _thickness = thickness;
                if (evt.customStyle.TryGetValue(s_Amplitude, out float amplitude)) _amplitude = amplitude;
                _lineColor = resolvedStyle.color;
                MarkDirtyRepaint(); 
            });
        }

        void DrawWaves(MeshGenerationContext mgc)
        {
            var painter = mgc.painter2D;
            painter.lineWidth = _thickness; 
            painter.strokeColor = _lineColor;

            painter.BeginPath();
            float startY = contentRect.height / 2;
            painter.MoveTo(new Vector2(0, startY));

            for (float x = 0; x <= contentRect.width; x += 1)
            {
                float y = startY + Mathf.Sin(x * 0.1f) * _amplitude;
                painter.LineTo(new Vector2(x, y));
            }
            painter.Stroke();
        }
    }
}