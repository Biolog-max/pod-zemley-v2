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
            // z-order: 1.artBg → 2.artImg → 3.animLayer → 4.dropHighlight
            Img(p, Pad, 155, CW, 600, Cv(.95f));                          // 1. background
            artImg = SprImg(p, Pad + 20, 165, CW - 40, 540, null);        // 2. artifact image
            artRT = artImg.GetComponent<RectTransform>();

            // 3. animation layer (ABOVE artImg, semi-transparent container)
            var animLayer = new GameObject("AnimLayer", typeof(RectTransform));
            animLayer.transform.SetParent(p, false);
            Pos(animLayer, Pad, 155, CW, 600);
            animRoot = animLayer.transform;

            // 4. drop highlight (topmost, receives drops)
            dropHighlight = Img(p, Pad, 155, CW, 600, new Color(.35f, .55f, .35f, .15f));
            dropHighlight.raycastTarget = true;
            dropHighlight.gameObject.SetActive(false);
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
        // All animations use animRoot (the gray artifact area) as parent
        // with stretch anchors so they scale correctly

        Transform animRoot; // set in Build to the artifact background area

        IEnumerator AnimRuler(string result)
        {
            // деревянная линейка растёт слева направо
            var bar = MkAnim(0, -30, 0, 16);
            bar.GetComponent<Image>().color = new Color(.55f, .35f, .15f, .95f);
            bar.anchorMin = new Vector2(0.05f, 0.35f);
            bar.anchorMax = new Vector2(0.05f, 0.35f);
            bar.pivot = new Vector2(0, 0.5f);

            // деления появляются по мере роста
            float targetW = 850f;
            int totalTicks = 16;
            float dur = 0.6f, t = 0;
            while (t < dur)
            {
                t += Time.deltaTime;
                float p = Mathf.SmoothStep(0, 1, t / dur);
                bar.sizeDelta = new Vector2(targetW * p, 16);
                yield return null;
            }

            // 8 делений
            for (int i = 0; i <= 8; i++)
            {
                float tx = targetW * i / 8f;
                bool major = (i % 2 == 0);
                var tick = MkAnim(tx, 0, 3, major ? 35 : 20);
                tick.GetComponent<Image>().color = new Color(.3f, .15f, .05f);
                tick.anchorMin = tick.anchorMax = new Vector2(0.05f, 0.35f);
                tick.anchoredPosition = new Vector2(tx, 20);
            }
            yield return new WaitForSeconds(0.15f);

            // красные маркеры скользят
            var m1 = MkAnim(targetW * 0.1f, 40, 6, 40);
            m1.GetComponent<Image>().color = RED;
            m1.anchorMin = m1.anchorMax = new Vector2(0.05f, 0.35f);

            var m2 = MkAnim(targetW * 0.1f, 40, 6, 40);
            m2.GetComponent<Image>().color = RED;
            m2.anchorMin = m2.anchorMax = new Vector2(0.05f, 0.35f);

            t = 0;
            while (t < 0.3f)
            {
                t += Time.deltaTime;
                float p = Mathf.SmoothStep(0, 1, t / 0.3f);
                m2.anchoredPosition = new Vector2(Mathf.Lerp(targetW * 0.1f, targetW * 0.85f, p), 40);
                yield return null;
            }

            // линия между маркерами
            var line = MkAnim(0, 45, targetW * 0.75f, 3);
            line.GetComponent<Image>().color = RED;
            line.anchorMin = line.anchorMax = new Vector2(0.05f, 0.35f);
            line.anchoredPosition = new Vector2(targetW * 0.1f, 45);
            line.pivot = new Vector2(0, 0.5f);

            yield return new WaitForSeconds(0.2f);
            yield return ShowBubble(result);
            yield return new WaitForSeconds(1.2f);
        }

        IEnumerator AnimScales(string result)
        {
            // стойка
            var pole = MkAnim(0, -30, 8, 200);
            pole.GetComponent<Image>().color = new Color(.45f, .35f, .2f);
            // перекладина
            var beam = MkAnim(0, 70, 300, 6);
            beam.GetComponent<Image>().color = new Color(.55f, .45f, .2f);
            // левая чаша
            var lPan = MkAnim(-120, 10, 80, 6);
            lPan.GetComponent<Image>().color = new Color(.5f, .4f, .15f);
            // правая чаша
            var rPan = MkAnim(120, 10, 80, 6);
            rPan.GetComponent<Image>().color = new Color(.5f, .4f, .15f);

            // качание с затуханием
            float dur = 1.5f, t = 0;
            while (t < dur)
            {
                t += Time.deltaTime;
                float decay = 1f - Mathf.Pow(t / dur, 0.6f);
                float angle = Mathf.Sin(t * 6f) * 20f * decay;
                beam.localRotation = Quaternion.Euler(0, 0, angle);
                float osc = Mathf.Sin(t * 6f) * 40f * decay;
                lPan.anchoredPosition = new Vector2(-120, 10 - osc);
                rPan.anchoredPosition = new Vector2(120, 10 + osc);
                yield return null;
            }
            beam.localRotation = Quaternion.Euler(0, 0, -3f);
            lPan.anchoredPosition = new Vector2(-120, 10 + 12);
            rPan.anchoredPosition = new Vector2(120, 10 - 12);

            yield return ShowBubble(result);
            yield return new WaitForSeconds(1.2f);
        }

        IEnumerator AnimCarbon(string result)
        {
            // 5 зелёных линий летят волнами
            int lineCount = 5;
            var lines = new RectTransform[lineCount];
            Color scanCol = new Color(.25f, .8f, .25f);
            for (int i = 0; i < lineCount; i++)
            {
                lines[i] = MkAnim(0, 0, 4, 450);
                lines[i].GetComponent<Image>().color = new Color(scanCol.r, scanCol.g, scanCol.b, 0);
            }

            // HUD badge
            var hud = MkAnim(-350, 200, 100, 35);
            hud.GetComponent<Image>().color = new Color(0, 0, 0, .6f);
            var hudTxt = new GameObject("_ht", typeof(RectTransform));
            hudTxt.transform.SetParent(hud, false);
            var htrt = hudTxt.GetComponent<RectTransform>();
            htrt.anchorMin = Vector2.zero; htrt.anchorMax = Vector2.one;
            htrt.offsetMin = htrt.offsetMax = Vector2.zero;
            var ht = hudTxt.AddComponent<Text>();
            ht.text = "C-14"; ht.font = Font.CreateDynamicFontFromOSFont("Arial", 14);
            ht.fontSize = 18; ht.color = new Color(.4f, .9f, .4f);
            ht.alignment = TextAnchor.MiddleCenter; ht.raycastTarget = false;
            animObjs.Add(hudTxt);

            float dur = 1.1f, t = 0;
            while (t < dur)
            {
                t += Time.deltaTime;
                float p = t / dur;
                for (int i = 0; i < lineCount; i++)
                {
                    float phase = (p * 2.5f + i * 0.18f) % 1f;
                    float x = Mathf.Lerp(-420, 420, phase);
                    float alpha = Mathf.Sin(phase * Mathf.PI) * 0.7f;
                    lines[i].anchoredPosition = new Vector2(x, 0);
                    lines[i].GetComponent<Image>().color = new Color(scanCol.r, scanCol.g, scanCol.b, alpha);
                }
                yield return null;
            }
            foreach (var l in lines) l.GetComponent<Image>().color = new Color(0, 0, 0, 0);

            yield return ShowBubble(result);
            yield return new WaitForSeconds(1.2f);
        }

        IEnumerator AnimDict(string result)
        {
            // лупа: тёмное кольцо + светлый центр
            var ring = MkAnim(-150, 0, 120, 120);
            ring.GetComponent<Image>().color = new Color(.3f, .25f, .15f, .85f);
            var inner = MkAnim(0, 0, 96, 96);
            inner.GetComponent<Image>().color = new Color(.9f, .9f, .85f, .3f);
            inner.SetParent(ring, false);
            inner.anchorMin = inner.anchorMax = new Vector2(.5f, .5f);
            inner.anchoredPosition = Vector2.zero;
            // ручка
            var handle = MkAnim(45, -45, 12, 60);
            handle.GetComponent<Image>().color = new Color(.4f, .3f, .15f);
            handle.SetParent(ring, false);
            handle.anchorMin = handle.anchorMax = new Vector2(.5f, .5f);
            handle.localRotation = Quaternion.Euler(0, 0, -45);

            // лупа летит по Безье
            Vector2 start = new Vector2(-200, -50), ctrl = new Vector2(0, 100), end = new Vector2(150, 50);
            float dur = 0.9f, t = 0;
            while (t < dur)
            {
                t += Time.deltaTime;
                float p = Mathf.SmoothStep(0, 1, t / dur);
                Vector2 a = Vector2.Lerp(start, ctrl, p);
                Vector2 b = Vector2.Lerp(ctrl, end, p);
                ring.anchoredPosition = Vector2.Lerp(a, b, p);
                float sc = 1f + 0.12f * Mathf.Sin(t * 7f);
                ring.localScale = Vector3.one * sc;
                yield return null;
            }
            ring.localScale = Vector3.one;
            // flash green
            inner.GetComponent<Image>().color = new Color(.8f, .95f, .8f, .5f);

            yield return new WaitForSeconds(0.2f);
            yield return ShowBubble(result);
            yield return new WaitForSeconds(1.2f);
        }

        IEnumerator ShowBubble(string text)
        {
            // background
            var bub = MkAnim(0, -80, 240, 55);
            bub.GetComponent<Image>().color = PRI;
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

        // Create animation element parented to animRoot (artifact background area)
        RectTransform MkAnim(float x, float y, float w, float h)
        {
            var go = new GameObject("_a", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(animRoot, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(w, h);
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
