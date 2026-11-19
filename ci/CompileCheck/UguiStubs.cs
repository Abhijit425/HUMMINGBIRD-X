// Minimal API surface of Unity's uGUI package (com.unity.ugui) used by HUMMINGBIRD X.
// Only used by the CI compile check (the real package is compiled by Unity in the actual build).
using UnityEngine;
using UnityEngine.Events;

namespace UnityEngine.EventSystems
{
    public class UIBehaviour : MonoBehaviour { }
    public class BaseEventData { }
    public class PointerEventData : BaseEventData { public Vector2 position; public Vector2 delta; public int pointerId; }
    public interface IEventSystemHandler { }
    public interface IPointerDownHandler : IEventSystemHandler { void OnPointerDown(PointerEventData e); }
    public interface IPointerUpHandler : IEventSystemHandler { void OnPointerUp(PointerEventData e); }
    public interface IPointerExitHandler : IEventSystemHandler { void OnPointerExit(PointerEventData e); }
    public interface IDragHandler : IEventSystemHandler { void OnDrag(PointerEventData e); }
    public class EventSystem : UIBehaviour
    {
        public static EventSystem current;
        public bool IsPointerOverGameObject() => false;
        public bool IsPointerOverGameObject(int pointerId) => false;
    }
    public class BaseInputModule : UIBehaviour { }
    public class StandaloneInputModule : BaseInputModule { }
    public class BaseRaycaster : UIBehaviour { }
}

namespace UnityEngine.UI
{
    using UnityEngine.EventSystems;

    public class Graphic : UIBehaviour
    {
        public virtual Color color { get; set; }
        public virtual bool raycastTarget { get; set; }
        public RectTransform rectTransform => null;
    }
    public class MaskableGraphic : Graphic { }
    public class Image : MaskableGraphic
    {
        public enum Type { Simple, Sliced, Tiled, Filled }
        public enum FillMethod { Horizontal, Vertical, Radial90, Radial180, Radial360 }
        public Sprite sprite { get; set; }
        public Type type { get; set; }
        public float pixelsPerUnitMultiplier { get; set; }
        public bool preserveAspect { get; set; }
        public FillMethod fillMethod { get; set; }
        public float fillAmount { get; set; }
    }
    public class RawImage : MaskableGraphic { public Texture texture { get; set; } }
    public class Text : MaskableGraphic
    {
        public Font font { get; set; }
        public virtual string text { get; set; }
        public int fontSize { get; set; }
        public TextAnchor alignment { get; set; }
        public HorizontalWrapMode horizontalOverflow { get; set; }
        public VerticalWrapMode verticalOverflow { get; set; }
        public bool supportRichText { get; set; }
        public virtual float preferredWidth => 0f;
    }
    public class BaseMeshEffect : UIBehaviour { }
    public class Shadow : BaseMeshEffect { public Color effectColor { get; set; } public Vector2 effectDistance { get; set; } }
    public class Outline : Shadow { }

    public struct ColorBlock
    {
        public Color normalColor { get; set; }
        public Color highlightedColor { get; set; }
        public Color pressedColor { get; set; }
        public Color selectedColor { get; set; }
        public Color disabledColor { get; set; }
        public float fadeDuration { get; set; }
        public float colorMultiplier { get; set; }
    }
    public class Selectable : UIBehaviour
    {
        public Graphic targetGraphic { get; set; }
        public ColorBlock colors { get; set; }
        public bool interactable { get; set; }
    }
    public class Button : Selectable
    {
        public class ButtonClickedEvent : UnityEvent { }
        public ButtonClickedEvent onClick { get; set; } = new ButtonClickedEvent();
    }
    public class Slider : Selectable
    {
        public enum Direction { LeftToRight, RightToLeft, BottomToTop, TopToBottom }
        public class SliderEvent : UnityEvent<float> { }
        public RectTransform fillRect { get; set; }
        public RectTransform handleRect { get; set; }
        public Direction direction { get; set; }
        public float minValue { get; set; }
        public float maxValue { get; set; }
        public virtual float value { get; set; }
        public SliderEvent onValueChanged { get; set; } = new SliderEvent();
    }
    public class ScrollRect : UIBehaviour
    {
        public enum MovementType { Unrestricted, Elastic, Clamped }
        public RectTransform content { get; set; }
        public RectTransform viewport { get; set; }
        public bool horizontal { get; set; }
        public bool vertical { get; set; }
        public MovementType movementType { get; set; }
    }
    public class RectMask2D : UIBehaviour { }
    public abstract class LayoutGroup : UIBehaviour
    {
        public RectOffset padding { get; set; }
        public TextAnchor childAlignment { get; set; }
    }
    public abstract class HorizontalOrVerticalLayoutGroup : LayoutGroup
    {
        public float spacing { get; set; }
        public bool childForceExpandWidth { get; set; }
        public bool childForceExpandHeight { get; set; }
        public bool childControlWidth { get; set; }
        public bool childControlHeight { get; set; }
    }
    public class VerticalLayoutGroup : HorizontalOrVerticalLayoutGroup { }
    public class HorizontalLayoutGroup : HorizontalOrVerticalLayoutGroup { }
    public class GridLayoutGroup : LayoutGroup { public Vector2 cellSize { get; set; } public Vector2 spacing { get; set; } }
    public class ContentSizeFitter : UIBehaviour
    {
        public enum FitMode { Unconstrained, MinSize, PreferredSize }
        public FitMode horizontalFit { get; set; }
        public FitMode verticalFit { get; set; }
    }
    public class CanvasScaler : UIBehaviour
    {
        public enum ScaleMode { ConstantPixelSize, ScaleWithScreenSize, ConstantPhysicalSize }
        public enum ScreenMatchMode { MatchWidthOrHeight, Expand, Shrink }
        public ScaleMode uiScaleMode { get; set; }
        public Vector2 referenceResolution { get; set; }
        public ScreenMatchMode screenMatchMode { get; set; }
        public float matchWidthOrHeight { get; set; }
    }
    public class GraphicRaycaster : BaseRaycaster { }
}
