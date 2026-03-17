using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using static History.UIKit;

namespace History
{
    public class ExamineScreen : BaseScreen
    {
        Text timerT, counterT, placedT, sideNameT, zoneDescT;
        Image artImg;
        RectTransform artRT;
        int sideIdx;

        // tool cards: combined tool + result
        GameObject[] cards = new GameObject[4];
        Text[] cardName = new Text[4];
        Text[] cardResult = new Text[4];
        Image[] cardBg = new Image[4];
        Image[] cardIcon = new Image[4];

        // drag overlay
        GameObject dragGhost;
        Image dropHighlight;
        bool animating;

        // procedural animation objects
        System.Collections.Generic.List<GameObject> animObjs = new System.Collections.Generic.List<GameObject>();

        protected override void Build(Transform p)
        {
            // top bar - compact
            Img(p, 0, 80, Width, 65, Cv(.95f));
            timerT   = Txt(p, "1:00", Pad, 83, 160, 58, 32, T1);
            counterT = Txt(p, "", 0, 83, Width, 58, 22, T2, TextAnchor.MiddleCenter);
            placedT  = Txt(p, "", Width - 300, 83, 260, 58, 18, T3, TextAnchor.MiddleRight);

            // artifact area - BIG (600px)
            Img(p, Pad, 155, CW, 600, Cv(.95f));
            artImg = SprImg(p, Pad + 20, 165, CW - 40, 540, null);
            artRT = artImg.GetComponent<RectTransform>();

            // drop highlight (hidden, shown when dragging over)
            dropHighlight = Img(p, Pad, 155, CW, 600, new Color(.35f, .55f, .35f, .15f));
            dropHighlight.raycastTarget = true;
            dropHighlight.gameObject.SetActive(false);
            // drop zone
            var dz = dropHighlight.gameObject.AddComponent<ToolDropZone>();
            dz.onDrop = OnToolDropped;

            // side nav overlaid on artifact bottom
            Img(p, Pad, 700, CW, 45, new Color(0, 0, 0, .3f));
            Btn(p, "\u25C0", Pad, 702, 80, 40, 24, false, () => SwipeSide(-1));
            sideNameT = Txt(p, "", 0, 702, Width, 40, 20, Color.white, TextAnchor.MiddleCenter);
            Btn(p, "\u25B6", Width - Pad - 80, 702, 80, 40, 24, false, () => SwipeSide(1));

            // zone description
            zoneDescT = Txt(p, "", Pad + 10, 755, CW - 20, 45, 18, T2);

            // --- TOOL CARDS (4 cards, tool + result merged) ---
            float cardW = (CW - 30) / 4; // 4 cards with 10px gaps
            string[] toolIds = null;
            if (gs.Config.tools.Count >= 4)
                toolIds = new string[] {
                    gs.Config.tools[0].icon, gs.Config.tools[1].icon,
                    gs.Config.tools[2].icon, gs.Config.tools[3].icon };

            for (int i = 0; i < 4; i++)
            {
                float cx = Pad + i * (cardW + 10);
                // card background
                var cardGo = new GameObject("Card" + i, typeof(RectTransform), typeof(Image));
                cardGo.transform.SetParent(p, false);
                Pos(cardGo, cx, 810, cardW, 145);
                var bg = cardGo.GetComponent<Image>();
                bg.color = CARD;
                cards[i] = cardGo;
                cardBg[i] = bg;

                // icon
                string icoPath = toolIds != null && i < toolIds.Length ? toolIds[i] : null;
                cardIcon[i] = SprImg(cardGo.transform, (cardW - 50) / 2, 10, 50, 50, icoPath);

                // tool name
                string tName = gs.Config.tools.Count > i ? gs.Config.tools[i].name : "";
                cardName[i] = Txt(cardGo.transform, tName, 0, 65, cardW, 25, 16, T1, TextAnchor.MiddleCenter);

                // result (or "тап!")
                cardResult[i] = Txt(cardGo.transform, "тап!", 0, 95, cardW, 40, 18, ACCENT, TextAnchor.MiddleCenter);

                // make card tappable AND draggable
                var btn = cardGo.AddComponent<Button>();
                int tapIdx = i;
                btn.onClick.AddListener(() => OnCardTapped(tapIdx));
                var drag = cardGo.AddComponent<ToolCardDrag>();
                drag.toolIdx = i;
                drag.examScreen = this;
            }

            // actions at bottom
            Btn(p, "Справочник", Pad, 975, CW / 2 - 10, 65, 20, false, () => gs.OpenRef());
            Btn(p, "Пропустить", Pad, 1060, (CW - 20) / 2, 80, 20, false, () => gs.SkipArt());
            Btn(p, "В МУЗЕЙ \u2192", Pad + CW / 2 + 10, 1060, (CW - 20) / 2, 80, 24, true, () => gs.GoMuseum());

            gs.OnToolPicked += OnToolPicked;
        }

        // --- SIDE NAVIGATION ---
        void SwipeSide(int dir)
        {
            if (gs.Art == null || gs.Art.zones.Count == 0) return;
            sideIdx = (sideIdx + dir + gs.Art.zones.Count) % gs.Art.zones.Count;
            ShowSide(true);
        }

        void ShowSide(bool animate)
        {
            var a = gs.Art;
            if (a == null || a.zones.Count == 0) return;
            var z = a.zones[sideIdx];
            sideNameT.text = z.name + " (" + (sideIdx + 1) + "/" + a.zones.Count + ")";
            if (!string.IsNullOrEmpty(z.image))
            {
                var spr = DataLoader.LoadSprite("Art/Artifacts/Sides/" + z.image);
                if (spr != null) { artImg.sprite = spr; artImg.color = Color.white; }
            }
            gs.ExamZone(z.id);
            zoneDescT.text = gs.SeenZones.Contains(z.id) ? z.description : "";
            if (animate && artRT != null && root.activeInHierarchy)
                ((MonoBehaviour)gs).StartCoroutine(CoFlip());
        }

        IEnumerator CoFlip()
        {
            float t = 0, d = 0.2f;
            while (t < d)
            {
                t += Time.deltaTime;
                float p = t / d;
                float sx = p < 0.5f ? 1f - p * 2f : (p - 0.5f) * 2f;
                artRT.localScale = new Vector3(sx, 1, 1);
                yield return null;
            }
            artRT.localScale = Vector3.one;
        }

        // --- TOOL INTERACTION ---
        void OnToolPicked(string tid) { RefreshCards(); }

        void OnCardTapped(int idx)
        {
            if (animating) return;
            if (gs.Config.tools.Count <= idx) return;
            string tid = gs.Config.tools[idx].id;
            if (gs.UsedTools.Contains(tid)) return;
            gs.SelectTool(tid);
            // apply immediately with animation
            ((MonoBehaviour)gs).StartCoroutine(CoToolAnim(tid));
        }

        public void OnDragStart(int toolIdx)
        {
            if (gs.Config.tools.Count <= toolIdx) return;
            string tid = gs.Config.tools[toolIdx].id;
            if (gs.UsedTools.Contains(tid)) return;
            gs.SelectTool(tid);
            dropHighlight.gameObject.SetActive(true);
        }

        public void OnDragEnd(int toolIdx)
        {
            // delay hiding so OnDrop can fire first
            ((MonoBehaviour)gs).StartCoroutine(CoDelayedHideDrop());
        }

        IEnumerator CoDelayedHideDrop()
        {
            yield return null; // wait one frame for OnDrop to process
            dropHighlight.gameObject.SetActive(false);
            if (gs.SelectedTool != null && !animating)
            {
                // dropped outside artifact — deselect
                gs.SelectTool(gs.SelectedTool); // toggles off
            }
        }

        void OnToolDropped(string toolId)
        {
            if (animating) return;
            if (gs.SelectedTool != null)
                ((MonoBehaviour)gs).StartCoroutine(CoToolAnim(gs.SelectedTool));
        }

        IEnumerator CoToolAnim(string toolId)
        {
            animating = true;
            var tool = gs.Config.tools.Find(x => x.id == toolId);
            if (tool == null) { animating = false; yield break; }

            string result = gs.Art.traits.Get(tool.reveals);
            ClearAnimObjs();

            // run specific animation
            switch (toolId)
            {
                case "ruler":     yield return AnimRuler(result); break;
                case "scales":    yield return AnimScales(result); break;
                case "carbon":    yield return AnimCarbon(result); break;
                case "dictionary":yield return AnimDict(result); break;
                default:          yield return new WaitForSeconds(0.5f); break;
            }

            // apply
            gs.ApplyTool();
            RefreshCards();
            ClearAnimObjs();
            animating = false;
        }

        // --- PROCEDURAL ANIMATIONS ---

        IEnumerator AnimRuler(string result)
        {
            // ruler bar grows across artifact
            var bar = MkRect(artImg.transform, 0, 0, 0, 10, new Color(.55f, .35f, .15f, .9f));
            bar.anchorMin = bar.anchorMax = new Vector2(0, 0.3f);
            bar.pivot = new Vector2(0, 0.5f);
            float fw = artRT.rect.width - 40;
            float t = 0;
            while (t < 0.5f) { t += Time.deltaTime; bar.sizeDelta = new Vector2(fw * (t / 0.5f), 10); yield return null; }
            // markers
            var m1 = MkRect(artImg.transform, 20, 0, 4, 30, RED); m1.anchorMin = m1.anchorMax = new Vector2(0, 0.3f);
            var m2 = MkRect(artImg.transform, fw - 20, 0, 4, 30, RED); m2.anchorMin = m2.anchorMax = new Vector2(0, 0.3f);
            yield return new WaitForSeconds(0.2f);
            // bubble
            yield return ShowBubble(result);
            yield return new WaitForSeconds(1f);
        }

        IEnumerator AnimScales(string result)
        {
            var beam = MkRect(artImg.transform, 0, 30, 200, 4, new Color(.5f, .4f, .2f));
            beam.anchorMin = beam.anchorMax = new Vector2(0.5f, 0.4f);
            float t = 0;
            while (t < 1f) {
                t += Time.deltaTime;
                float angle = Mathf.Sin(t * 7f) * 15f * (1f - t);
                beam.localRotation = Quaternion.Euler(0, 0, angle);
                yield return null;
            }
            beam.localRotation = Quaternion.identity;
            yield return ShowBubble(result);
            yield return new WaitForSeconds(1f);
        }

        IEnumerator AnimCarbon(string result)
        {
            var lines = new RectTransform[4];
            float h = artRT.rect.height;
            for (int i = 0; i < 4; i++) {
                lines[i] = MkRect(artImg.transform, 0, 0, 3, h * 0.8f, new Color(.3f, .8f, .3f, 0));
                lines[i].anchorMin = lines[i].anchorMax = new Vector2(0, 0.5f);
            }
            float fw = artRT.rect.width, t = 0;
            while (t < 0.9f) {
                t += Time.deltaTime;
                for (int i = 0; i < 4; i++) {
                    float phase = (t * 2f + i * 0.2f) % 1f;
                    lines[i].anchoredPosition = new Vector2(fw * phase, 0);
                    lines[i].GetComponent<Image>().color = new Color(.3f, .8f, .3f, Mathf.Sin(phase * Mathf.PI) * 0.6f);
                }
                yield return null;
            }
            foreach (var l in lines) l.GetComponent<Image>().color = new Color(0, 0, 0, 0);
            yield return ShowBubble(result);
            yield return new WaitForSeconds(1f);
        }

        IEnumerator AnimDict(string result)
        {
            var lens = MkRect(artImg.transform, 0, 0, 90, 90, new Color(.3f, .25f, .2f, .6f));
            lens.anchorMin = lens.anchorMax = new Vector2(0.3f, 0.5f);
            float t = 0;
            Vector2 start = new Vector2(-80, 0), end = new Vector2(80, 30);
            while (t < 0.7f) {
                t += Time.deltaTime;
                float p = Mathf.SmoothStep(0, 1, t / 0.7f);
                lens.anchoredPosition = Vector2.Lerp(start, end, p);
                float sc = 1f + 0.1f * Mathf.Sin(t * 8f);
                lens.localScale = Vector3.one * sc;
                yield return null;
            }
            lens.localScale = Vector3.one;
            yield return ShowBubble(result);
            yield return new WaitForSeconds(1f);
        }

        IEnumerator ShowBubble(string text)
        {
            // background
            var bub = MkRect(artImg.transform, 0, -20, 240, 55, PRI);
            bub.anchorMin = bub.anchorMax = new Vector2(0.5f, 0.35f);
            // text as child
            var txtGo = new GameObject("_bt", typeof(RectTransform));
            txtGo.transform.SetParent(bub, false);
            var trt = txtGo.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
            var txt = txtGo.AddComponent<Text>();
            txt.text = text;
            txt.font = Font.CreateDynamicFontFromOSFont("Arial", 14);
            txt.fontSize = 28;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.raycastTarget = false;
            animObjs.Add(txtGo);
            // pop animation
            bub.localScale = Vector3.zero;
            float t = 0;
            while (t < 0.35f)
            {
                t += Time.deltaTime;
                float p = t / 0.35f;
                float s = p < 0.6f ? (p / 0.6f) * 1.2f : 1.2f - (p - 0.6f) / 0.4f * 0.2f;
                bub.localScale = Vector3.one * s;
                yield return null;
            }
            bub.localScale = Vector3.one;
        }

        RectTransform MkRect(Transform parent, float x, float y, float w, float h, Color col)
        {
            var go = new GameObject("_a", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(w, h);
            go.GetComponent<Image>().color = col;
            go.GetComponent<Image>().raycastTarget = false;
            animObjs.Add(go);
            return rt;
        }

        void ClearAnimObjs()
        {
            foreach (var go in animObjs) if (go != null) Object.Destroy(go);
            animObjs.Clear();
        }

        // --- REFRESH ---
        void RefreshCards()
        {
            string[] traitIds = { "size", "year", "weight", "textLanguage" };
            for (int i = 0; i < 4 && i < gs.Config.tools.Count; i++)
            {
                var tool = gs.Config.tools[i];
                bool used = gs.UsedTools.Contains(tool.id);
                cardBg[i].color = used ? Cv(.92f) : CARD;
                if (used && gs.Art != null)
                {
                    cardResult[i].text = gs.Art.traits.Get(tool.reveals) + " \u2713";
                    cardResult[i].color = ACCENT;
                }
                else
                {
                    bool sel = gs.SelectedTool == tool.id;
                    cardResult[i].text = sel ? "отпустите!" : "тап!";
                    cardResult[i].color = sel ? RED : T3;
                }
            }
        }

        public void UpdateTimer()
        {
            if (!gs.TimerOn || timerT == null) return;
            int m = (int)(gs.TimeLeft / 60), s = (int)(gs.TimeLeft % 60);
            timerT.text = m + ":" + s.ToString("D2");
            timerT.color = gs.TimeLeft < 10 ? RED : T1;
        }

        public override void Refresh()
        {
            var a = gs.Art;
            if (a == null) return;
            counterT.text = "Артефакт " + (gs.ArtIdx + 1) + " / " + gs.Total;
            placedT.text = "Размещено: " + gs.PlacedN;
            sideIdx = 0;
            ShowSide(false);
            RefreshCards();
            gs.OnTrait -= OnTraitReveal;
            gs.OnTrait += OnTraitReveal;
        }

        void OnTraitReveal(string t, string v) { RefreshCards(); }
    }

    // --- DRAG COMPONENT FOR TOOL CARDS ---
    public class ToolCardDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public int toolIdx;
        public ExamineScreen examScreen;
        GameObject ghost;

        public void OnBeginDrag(PointerEventData e)
        {
            examScreen.OnDragStart(toolIdx);
            ghost = new GameObject("Ghost", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            ghost.transform.SetParent(transform.root, false);
            ghost.transform.SetAsLastSibling();
            var img = ghost.GetComponent<Image>();
            var src = GetComponentInChildren<Image>();
            if (src != null && src.sprite != null) { img.sprite = src.sprite; img.preserveAspect = true; }
            else img.color = new Color(.3f, .3f, .3f, .6f);
            ghost.GetComponent<RectTransform>().sizeDelta = new Vector2(90, 90);
            ghost.GetComponent<CanvasGroup>().blocksRaycasts = false;
            ghost.GetComponent<CanvasGroup>().alpha = 0.75f;
            ghost.transform.position = e.position;
        }

        public void OnDrag(PointerEventData e)
        {
            if (ghost != null) ghost.transform.position = e.position;
        }

        public void OnEndDrag(PointerEventData e)
        {
            if (ghost != null) Destroy(ghost);
            examScreen.OnDragEnd(toolIdx);
        }
    }

    // --- DROP ZONE ON ARTIFACT ---
    public class ToolDropZone : MonoBehaviour, IDropHandler
    {
        public System.Action<string> onDrop;
        public void OnDrop(PointerEventData e)
        {
            var gs = GameState.I;
            if (gs != null && gs.SelectedTool != null)
                onDrop?.Invoke(gs.SelectedTool);
        }
    }
}
