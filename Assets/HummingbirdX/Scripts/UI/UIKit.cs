using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HummingbirdX
{
    /// <summary>Builders for the minimalist dark-glass interface.</summary>
    public static class UIKit
    {
        public static readonly Color Glass = new Color(0.04f, 0.07f, 0.06f, 0.62f);
        public static readonly Color GlassLight = new Color(0.12f, 0.18f, 0.16f, 0.55f);
        public static readonly Color Accent = new Color(0.55f, 1f, 0.78f, 1f);
        public static readonly Color TextColor = new Color(0.94f, 0.97f, 0.95f, 1f);
        public static readonly Color TextDim = new Color(0.94f, 0.97f, 0.95f, 0.6f);
        public static readonly Color Stroke = new Color(1f, 1f, 1f, 0.10f);

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        public static RectTransform Place(this RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Stretch(this RectTransform rt, float l = 0, float b = 0, float r = 0, float t = 0)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(l, b); rt.offsetMax = new Vector2(-r, -t);
            return rt;
        }

        public static Image Panel(Transform parent, string name, Color? color = null, bool stroke = true)
        {
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = Res.Sprite("ui_round", 32f);
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 2.2f;
            img.color = color ?? Glass;
            if (stroke)
            {
                var o = rt.gameObject.AddComponent<Outline>();
                o.effectColor = Stroke;
                o.effectDistance = new Vector2(1f, -1f);
            }
            return img;
        }

        public static Text Label(Transform parent, string text, int size, Color? color = null, TextAnchor align = TextAnchor.MiddleLeft, Font font = null)
        {
            var rt = Rect("Label", parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = font ?? Res.Regular;
            t.text = text;
            t.fontSize = size;
            t.color = color ?? TextColor;
            t.alignment = align;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            t.supportRichText = true;
            return t;
        }

        public static Image Icon(Transform parent, string sprite, float size, Color? color = null)
        {
            var rt = Rect("Icon", parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = Res.Sprite(sprite);
            img.color = color ?? TextColor;
            img.raycastTarget = false;
            img.preserveAspect = true;
            rt.sizeDelta = new Vector2(size, size);
            return img;
        }

        public static Button Button(Transform parent, string name, Action onClick, Color? bg = null)
        {
            var img = Panel(parent, name, bg ?? GlassLight);
            var b = img.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            var cb = b.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.1f, 1.1f, 1.1f, 1f);
            cb.pressedColor = new Color(0.75f, 0.9f, 0.85f, 1f);
            cb.selectedColor = Color.white;
            cb.fadeDuration = 0.08f;
            b.colors = cb;
            b.onClick.AddListener(() =>
            {
                AudioManager.I?.PlayUI("sfx_tap", 0.35f);
                onClick?.Invoke();
            });
            img.gameObject.AddComponent<PressScale>();
            return b;
        }

        public static Button TextButton(Transform parent, string text, Action onClick, int size = 30, Color? bg = null)
        {
            var b = Button(parent, text, onClick, bg);
            var l = Label(b.transform, text, size, TextColor, TextAnchor.MiddleCenter, Res.SemiBold);
            l.rectTransform.Stretch();
            return b;
        }

        public static Button IconButton(Transform parent, string icon, Action onClick, float size, string caption = null, Color? bg = null)
        {
            var b = Button(parent, icon, onClick, bg ?? Glass);
            ((RectTransform)b.transform).sizeDelta = new Vector2(size, size);
            var i = Icon(b.transform, icon, size * 0.5f);
            if (caption != null)
            {
                i.rectTransform.anchoredPosition = new Vector2(0, size * 0.08f);
                var l = Label(b.transform, caption, Mathf.RoundToInt(size * 0.16f), TextDim, TextAnchor.MiddleCenter, Res.SemiBold);
                l.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, size * 0.06f), new Vector2(size, size * 0.25f));
            }
            return b;
        }

        public static Slider Slider(Transform parent, string caption, float min, float max, float value, Action<float> onChange, float width = 520f)
        {
            var row = Rect(caption, parent);
            row.sizeDelta = new Vector2(width, 70);
            var cap = Label(row, caption, 24, TextDim);
            cap.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), Vector2.zero, new Vector2(width * 0.36f, 40));

            var sRt = Rect("Slider", row);
            sRt.Place(new Vector2(1, 0.5f), new Vector2(1, 0.5f), Vector2.zero, new Vector2(width * 0.6f, 40));
            var s = sRt.gameObject.AddComponent<Slider>();
            var bg = Panel(sRt, "Track", new Color(1, 1, 1, 0.12f), false);
            bg.rectTransform.Stretch(0, 16, 0, 16);
            var fillArea = Rect("FillArea", sRt); fillArea.Stretch(0, 16, 0, 16);
            var fill = Panel(fillArea, "Fill", Accent, false);
            fill.rectTransform.Stretch();
            var handleArea = Rect("HandleArea", sRt); handleArea.Stretch(14, 0, 14, 0);
            var handle = Rect("Handle", handleArea);
            var hi = handle.gameObject.AddComponent<Image>();
            hi.sprite = Res.Sprite("ui_circle");
            hi.color = Color.white;
            handle.sizeDelta = new Vector2(34, 34);
            s.fillRect = fill.rectTransform;
            s.handleRect = handle;
            s.targetGraphic = hi;
            s.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            s.minValue = min; s.maxValue = max; s.value = value;
            s.onValueChanged.AddListener(v => onChange?.Invoke(v));
            return s;
        }

        public static Button Toggle(Transform parent, string caption, bool initial, Action<bool> onChange, float width = 520f)
        {
            bool state = initial;
            var row = Rect(caption, parent);
            row.sizeDelta = new Vector2(width, 64);
            var cap = Label(row, caption, 24, TextDim);
            cap.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), Vector2.zero, new Vector2(width - 120, 40));
            Image knob = null, track = null;
            RectTransform k = null;
            Button btn = null;
            btn = Button(row, "Switch", () =>
            {
                state = !state;
                Refresh();
                onChange?.Invoke(state);
            }, new Color(1, 1, 1, 0.14f));
            track = (Image)btn.targetGraphic;
            ((RectTransform)btn.transform).Place(new Vector2(1, 0.5f), new Vector2(1, 0.5f), Vector2.zero, new Vector2(96, 48));
            k = Rect("Knob", btn.transform);
            knob = k.gameObject.AddComponent<Image>();
            knob.sprite = Res.Sprite("ui_circle");
            knob.raycastTarget = false;
            k.sizeDelta = new Vector2(38, 38);
            void Refresh()
            {
                k.Place(new Vector2(state ? 1 : 0, 0.5f), new Vector2(state ? 1 : 0, 0.5f), new Vector2(state ? -5 : 5, 0), new Vector2(38, 38));
                track.color = state ? new Color(Accent.r, Accent.g, Accent.b, 0.75f) : new Color(1, 1, 1, 0.14f);
            }
            Refresh();
            return btn;
        }

        public static void VStack(RectTransform rt, float spacing, TextAnchor align = TextAnchor.UpperCenter, int pad = 0)
        {
            var v = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing;
            v.childAlignment = align;
            v.childControlWidth = false; v.childControlHeight = false;
            v.childForceExpandWidth = false; v.childForceExpandHeight = false;
            v.padding = new RectOffset(pad, pad, pad, pad);
        }

        public static void HStack(RectTransform rt, float spacing, TextAnchor align = TextAnchor.MiddleCenter)
        {
            var h = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = spacing;
            h.childAlignment = align;
            h.childControlWidth = false; h.childControlHeight = false;
            h.childForceExpandWidth = false; h.childForceExpandHeight = false;
        }

        public static CanvasGroup Group(GameObject go)
        {
            var g = go.GetComponent<CanvasGroup>();
            return g ? g : go.AddComponent<CanvasGroup>();
        }

        public static IEnumerator Fade(CanvasGroup g, float to, float dur, bool deactivate = false)
        {
            if (to > 0f) g.gameObject.SetActive(true);
            float from = g.alpha, t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                g.alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0, 1, t / dur));
                yield return null;
            }
            g.alpha = to;
            g.interactable = g.blocksRaycasts = to > 0.5f;
            if (deactivate && to <= 0f) g.gameObject.SetActive(false);
        }
    }

    /// <summary>Subtle press animation on buttons.</summary>
    public class PressScale : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        float target = 1f;
        public void OnPointerDown(PointerEventData e) { target = 0.94f; }
        public void OnPointerUp(PointerEventData e) { target = 1f; }
        void Update()
        {
            float s = Mathf.Lerp(transform.localScale.x, target, 1f - Mathf.Exp(-Time.unscaledDeltaTime * 20f));
            transform.localScale = new Vector3(s, s, 1f);
        }
    }

    /// <summary>Hold-type button (up/down/boost) reporting its pressed state.</summary>
    public class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public Action<bool> OnChange;
        public bool Held { get; private set; }
        public void OnPointerDown(PointerEventData e) { Held = true; OnChange?.Invoke(true); UIManager.I?.Poke(); }
        public void OnPointerUp(PointerEventData e) { Held = false; OnChange?.Invoke(false); }
        public void OnPointerExit(PointerEventData e) { if (Held) { Held = false; OnChange?.Invoke(false); } }
        void OnDisable() { if (Held) { Held = false; OnChange?.Invoke(false); } }
    }

    /// <summary>
    /// Virtual joystick. Floating mode: the stick appears wherever the thumb lands inside its zone.
    /// </summary>
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public RectTransform baseRt, knob;
        public bool floating = true;
        public float radius = 110f;
        public Vector2 Value { get; private set; }
        public Action<Vector2> OnValue;
        Vector2 home;
        RectTransform zone;
        Camera uiCam;

        public void Init(RectTransform zoneRt, RectTransform b, RectTransform k)
        {
            zone = zoneRt; baseRt = b; knob = k;
            home = b.anchoredPosition;
        }

        public void OnPointerDown(PointerEventData e)
        {
            UIManager.I?.Poke();
            if (floating && RectTransformUtility.ScreenPointToLocalPointInRectangle(zone, e.position, uiCam, out var lp))
            {
                var parent = (RectTransform)baseRt.parent;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, e.position, uiCam, out var pp);
                baseRt.localPosition = pp;
            }
            OnDrag(e);
        }

        public void OnDrag(PointerEventData e)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(baseRt, e.position, uiCam, out var lp)) return;
            Vector2 v = Vector2.ClampMagnitude(lp, radius);
            knob.anchoredPosition = v;
            Value = v / radius;
            // small dead zone and a gentle response curve for fine hovering control
            float m = Value.magnitude;
            Value = m < 0.08f ? Vector2.zero : Value.normalized * Mathf.Pow((m - 0.08f) / 0.92f, 1.25f);
            OnValue?.Invoke(Value);
        }

        public void OnPointerUp(PointerEventData e)
        {
            knob.anchoredPosition = Vector2.zero;
            Value = Vector2.zero;
            OnValue?.Invoke(Value);
            if (floating) baseRt.anchoredPosition = home;
        }

        void OnDisable()
        {
            if (knob) knob.anchoredPosition = Vector2.zero;
            Value = Vector2.zero;
            OnValue?.Invoke(Value);
        }
    }
}
