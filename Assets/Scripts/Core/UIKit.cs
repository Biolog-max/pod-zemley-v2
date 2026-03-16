using System;
using UnityEngine;
using UnityEngine.UI;

namespace History
{
    public static class UIKit
    {
        const float W = 1080f, P = 40f;
        static Font _f;
        static Font F => _f != null ? _f : (_f = Font.CreateDynamicFontFromOSFont("Arial", 14));
        public static float Width => W;
        public static float Pad => P;
        public static float CW => W - P * 2;

        public static Color BG   = Cv(.96f), CARD = Cv(.97f), WHITE = Cv(1f);
        public static Color PRI  = Cv(.20f), SEC  = Cv(.94f), DIS = Cv(.88f);
        public static Color T1   = Cv(.13f), T2   = Cv(.53f), T3 = Cv(.73f);
        public static Color DIV  = Cv(.93f), RED = new Color(.8f,.2f,.2f);
        public static Color ACCENT = new Color(.35f,.55f,.35f);
        public static Color Cv(float v) { return new Color(v, v, v); }

        public static RectTransform Pos(GameObject go, float x, float y, float w, float h)
        {
            var rt = go.GetComponent<RectTransform>();
            if (rt == null) rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);
            return rt;
        }

        public static RectTransform PosBot(GameObject go, float x, float up, float w, float h)
        {
            var rt = go.GetComponent<RectTransform>();
            if (rt == null) rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0, 0);
            rt.pivot = new Vector2(0, 0);
            rt.anchoredPosition = new Vector2(x, up);
            rt.sizeDelta = new Vector2(w, h);
            return rt;
        }

        public static Text Txt(Transform p, string text, float x, float y, float w, float h,
            int sz = 24, Color? col = null, TextAnchor align = TextAnchor.MiddleLeft)
        {
            var go = new GameObject("t", typeof(RectTransform));
            go.transform.SetParent(p, false);
            Pos(go, x, y, w, h);
            var t = go.AddComponent<Text>();
            t.text = text;
            t.font = F;
            t.fontSize = sz;
            t.color = col ?? T1;
            t.alignment = align;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        public static Button Btn(Transform p, string label, float x, float y, float w, float h,
            int sz = 26, bool pri = true, Action onClick = null)
        {
            var go = new GameObject("b", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(p, false);
            Pos(go, x, y, w, h);
            go.GetComponent<Image>().color = pri ? PRI : SEC;
            var txt = Txt(go.transform, label, 0, 0, w, h, sz, pri ? Color.white : T1, TextAnchor.MiddleCenter);
            var trt = txt.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;
            var btn = go.GetComponent<Button>();
            if (onClick != null) btn.onClick.AddListener(() => onClick());
            return btn;
        }

        public static Image Img(Transform p, float x, float y, float w, float h, Color col)
        {
            var go = new GameObject("i", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(p, false);
            Pos(go, x, y, w, h);
            var img = go.GetComponent<Image>();
            img.color = col;
            img.raycastTarget = false;
            return img;
        }

        public static Image SprImg(Transform p, float x, float y, float w, float h, string path)
        {
            var go = new GameObject("s", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(p, false);
            Pos(go, x, y, w, h);
            var img = go.GetComponent<Image>();
            img.raycastTarget = false;
            img.preserveAspect = true;
            if (!string.IsNullOrEmpty(path))
            {
                var spr = DataLoader.LoadSprite(path);
                if (spr != null) { img.sprite = spr; img.color = Color.white; }
                else img.color = Cv(.9f);
            }
            else img.color = Cv(.9f);
            return img;
        }

        public static GameObject Panel(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = BG;
            go.SetActive(false);
            return go;
        }
    }
}
