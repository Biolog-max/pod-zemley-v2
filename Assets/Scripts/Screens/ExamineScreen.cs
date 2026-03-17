using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using static History.UIKit;

namespace History
{
    /// Examine screen: "Archaeologist's desk"
    /// Artifact fills 75% of screen, tools on edges, notebook = bottom drawer
    public class ExamineScreen : BaseScreen
    {
        // top bar
        Text timerT, counterT, placedT;
        // artifact
        Image artImg;
        RectTransform artRT;
        Text sideNameT;
        int sideIdx;
        // tool buttons on desk edges
        GameObject[] toolSlots = new GameObject[4];
        Image[] toolIcons = new Image[4];
        Text[] toolLabels = new Text[4];
        // animation layer
        Transform animRoot;
        List<GameObject> animObjs = new List<GameObject>();
        bool animating;
        // drawer (notebook)
        RectTransform drawerRT;
        bool drawerOpen;
        Text[] noteVal = new Text[5];
        Text zoneDescT;

        protected override void Build(Transform p)
        {
            // ===== TOP BAR (50px) =====
            Img(p, 0, 80, Width, 50, new Color(0, 0, 0, .35f));
            timerT   = Txt(p, "1:00", Pad, 82, 140, 46, 30, Color.white);
            counterT = Txt(p, "", 0, 82, Width, 46, 20, new Color(1,1,1,.7f), TextAnchor.MiddleCenter);
            placedT  = Txt(p, "", Width-260, 82, 220, 46, 16, new Color(1,1,1,.5f), TextAnchor.MiddleRight);

            // ===== DESK BACKGROUND (fills most of screen) =====
            Img(p, 0, 130, Width, 1300, new Color(.38f, .28f, .18f)); // dark wood
            // lighter wood grain overlay
            Img(p, 10, 140, Width-20, 1280, new Color(.45f, .33f, .20f));

            // ===== ARTIFACT (huge, centered) =====
            // shadow under artifact
            Img(p, 90, 220, Width-180, 730, new Color(0,0,0,.2f));
            // artifact image
            artImg = SprImg(p, 80, 200, Width-160, 740, null);
            artRT = artImg.GetComponent<RectTransform>();
            artImg.GetComponent<Image>().raycastTarget = true; // for tap

            // animation layer ABOVE artifact
            var animGo = new GameObject("AnimLayer", typeof(RectTransform));
            animGo.transform.SetParent(p, false);
            Pos(animGo, 80, 200, Width-160, 740);
            animRoot = animGo.transform;

            // drop zone (invisible, ABOVE anim layer)
            var dropImg = Img(p, 60, 180, Width-120, 780, new Color(.35f,.55f,.35f, 0));
            dropImg.raycastTarget = true;
            var dz = dropImg.gameObject.AddComponent<ToolDropZone>();
            dz.onDrop = OnToolDropped;
            // also tap
            var tapBtn = dropImg.gameObject.AddComponent<Button>();
            tapBtn.onClick.AddListener(OnArtifactTapped);

            // side navigation strip at bottom of artifact
            Img(p, 60, 920, Width-120, 50, new Color(0,0,0,.4f));
            Btn(p, "\u25C0", 70, 925, 70, 40, 22, false, () => SwipeSide(-1))
                .GetComponent<Image>().color = new Color(1,1,1,.15f);
            sideNameT = Txt(p, "", 0, 925, Width, 40, 18, Color.white, TextAnchor.MiddleCenter);
            Btn(p, "\u25B6", Width-140, 925, 70, 40, 22, false, () => SwipeSide(1))
                .GetComponent<Image>().color = new Color(1,1,1,.15f);

            // zone description (on desk, under artifact)
            zoneDescT = Txt(p, "", Pad+10, 980, CW-20, 40, 16, new Color(.85f,.80f,.70f));

            // ===== TOOLS on desk edges =====
            float toolY = 1030;
            float toolW = (Width - Pad*2 - 30) / 4;
            string[] toolIcoPaths = null;
            if (gs.Config.tools.Count >= 4)
                toolIcoPaths = new string[] {
                    gs.Config.tools[0].icon, gs.Config.tools[1].icon,
                    gs.Config.tools[2].icon, gs.Config.tools[3].icon };

            for (int i = 0; i < 4; i++)
            {
                float tx = Pad + i * (toolW + 10);
                // tool "sitting on desk"
                var slot = new GameObject("Tool" + i, typeof(RectTransform), typeof(Image));
                slot.transform.SetParent(p, false);
                Pos(slot, tx, toolY, toolW, 100);
                slot.GetComponent<Image>().color = new Color(.5f,.38f,.22f); // wood
                toolSlots[i] = slot;

                // icon
                string path = toolIcoPaths != null ? toolIcoPaths[i] : null;
                toolIcons[i] = SprImg(slot.transform, (toolW-50)/2, 5, 50, 50, path);

                // label + result
                toolLabels[i] = Txt(slot.transform, "", 0, 58, toolW, 35, 14, new Color(.9f,.85f,.75f), TextAnchor.MiddleCenter);

                // make tappable + draggable
                var btn = slot.AddComponent<Button>();
                int idx = i;
                btn.onClick.AddListener(() => OnToolTapped(idx));
                var drag = slot.AddComponent<ToolCardDrag>();
                drag.toolIdx = i;
                drag.examScreen = this;
            }

            // ===== DRAWER (notebook, slides up from bottom) =====
            var drawer = new GameObject("Drawer", typeof(RectTransform), typeof(Image));
            drawer.transform.SetParent(p, false);
            drawerRT = drawer.GetComponent<RectTransform>();
            drawerRT.anchorMin = new Vector2(0, 0);
            drawerRT.anchorMax = new Vector2(1, 0);
            drawerRT.pivot = new Vector2(0.5f, 0);
            drawerRT.anchoredPosition = new Vector2(0, 0);
            drawerRT.sizeDelta = new Vector2(0, 500);
            drawer.GetComponent<Image>().color = new Color(.95f, .93f, .88f);
            var dt = drawer.transform;

            // drawer handle
            Img(dt, Width/2 - 50, 490, 100, 6, new Color(.7f,.65f,.55f));
            // tap handle to toggle drawer
            var handleBtn = Img(dt, 0, 470, Width, 40, new Color(0,0,0,0));
            handleBtn.raycastTarget = true;
            handleBtn.gameObject.AddComponent<Button>().onClick.AddListener(ToggleDrawer);
            Txt(dt, "\u25B2 Блокнот", 0, 475, Width, 30, 16, new Color(.5f,.45f,.35f), TextAnchor.MiddleCenter);

            // notebook content
            string[] labels = {"Материал:", "Размер:", "Вес:", "Год:", "Язык:"};
            for (int i = 0; i < 5; i++)
            {
                float ny = 80 + i * 55;
                Txt(dt, labels[i], 40, ny, 300, 40, 22, new Color(.5f,.45f,.35f));
                noteVal[i] = Txt(dt, "\u2014", 350, ny, 600, 40, 22, new Color(.2f,.15f,.1f));
                if (i < 4) Img(dt, 40, ny+45, Width-80, 1, new Color(.85f,.82f,.75f));
            }

            // action buttons in drawer
            Btn(dt, "Справочник", 40, 365, (Width-100)/2, 65, 20, false, () => gs.OpenRef());
            Btn(dt, "В МУЗЕЙ \u2192", Width/2+10, 365, (Width-100)/2, 65, 24, true, () => gs.GoMuseum());
            Btn(dt, "Пропустить", 40, 440, Width-80, 45, 18, false, () => gs.SkipArt());

            // start drawer closed (only handle visible)
            drawerRT.anchoredPosition = new Vector2(0, -460);
            drawerOpen = false;

            gs.OnToolPicked += OnToolPicked;
        }

        // ===== DRAWER =====
        void ToggleDrawer()
        {
            drawerOpen = !drawerOpen;
            ((MonoBehaviour)gs).StartCoroutine(CoSlideDrawer(drawerOpen ? 0 : -460));
        }

        IEnumerator CoSlideDrawer(float targetY)
        {
            float startY = drawerRT.anchoredPosition.y;
            float t = 0, dur = 0.3f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float p = Mathf.SmoothStep(0, 1, t / dur);
                drawerRT.anchoredPosition = new Vector2(0, Mathf.Lerp(startY, targetY, p));
                yield return null;
            }
            drawerRT.anchoredPosition = new Vector2(0, targetY);
        }

        // ===== SIDE NAVIGATION =====
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
            sideNameT.text = z.name + " (" + (sideIdx+1) + "/" + a.zones.Count + ")";
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
            float t = 0;
            while (t < 0.2f) { t += Time.deltaTime; artRT.localScale = new Vector3(1f - (t/0.1f > 1 ? 2f-t/0.1f : t/0.1f), 1, 1); yield return null; }
            artRT.localScale = Vector3.one;
        }

        // ===== TOOL INTERACTION =====
        void OnToolTapped(int idx)
        {
            if (animating || gs.Config.tools.Count <= idx) return;
            string tid = gs.Config.tools[idx].id;
            if (gs.UsedTools.Contains(tid)) return;
            gs.SelectTool(tid);
            ((MonoBehaviour)gs).StartCoroutine(CoToolAnim(tid));
        }

        void OnToolPicked(string tid) { RefreshToolSlots(); }

        public void OnDragStart(int idx)
        {
            if (gs.Config.tools.Count <= idx) return;
            string tid = gs.Config.tools[idx].id;
            if (gs.UsedTools.Contains(tid)) return;
            gs.SelectTool(tid);
        }

        public void OnDragEnd(int idx) { }

        void OnToolDropped(string toolId)
        {
            if (animating || gs.SelectedTool == null) return;
            ((MonoBehaviour)gs).StartCoroutine(CoToolAnim(gs.SelectedTool));
        }

        void OnArtifactTapped()
        {
            if (animating || gs.SelectedTool == null) return;
            ((MonoBehaviour)gs).StartCoroutine(CoToolAnim(gs.SelectedTool));
        }

        IEnumerator CoToolAnim(string toolId)
        {
            animating = true;
            var tool = gs.Config.tools.Find(x => x.id == toolId);
            if (tool == null) { animating = false; yield break; }
            string result = gs.Art.traits.Get(tool.reveals);
            ClearAnim();

            switch (toolId)
            {
                case "ruler":     yield return AnimRuler(result); break;
                case "scales":    yield return AnimScales(result); break;
                case "carbon":    yield return AnimCarbon(result); break;
                case "dictionary":yield return AnimDict(result); break;
            }

            gs.ApplyTool();
            RefreshToolSlots();
            RefreshNote();
            ClearAnim();
            animating = false;
        }

        // ===== ANIMATIONS =====
        IEnumerator AnimRuler(string result)
        {
            var bar = Mk(0, 0, 0, 14); bar.GetComponent<Image>().color = new Color(.6f,.4f,.2f,.9f);
            bar.pivot = new Vector2(0, .5f); bar.anchoredPosition = new Vector2(-380, 0);
            float fw = 760f, t = 0;
            while (t < .5f) { t += Time.deltaTime; bar.sizeDelta = new Vector2(fw*t/.5f, 14); yield return null; }
            for (int i = 0; i <= 8; i++)
            {
                var tick = Mk(-380 + fw*i/8f, 18, 3, i%2==0 ? 30 : 16);
                tick.GetComponent<Image>().color = new Color(.35f,.2f,.1f);
                tick.pivot = new Vector2(.5f, 0);
            }
            yield return new WaitForSeconds(.15f);
            var m1 = Mk(-380 + fw*.1f, 30, 5, 35); m1.GetComponent<Image>().color = RED;
            var m2 = Mk(-380 + fw*.1f, 30, 5, 35); m2.GetComponent<Image>().color = RED;
            t = 0;
            while (t < .3f) { t += Time.deltaTime; m2.anchoredPosition = new Vector2(Mathf.Lerp(-380+fw*.1f, -380+fw*.85f, t/.3f), 30); yield return null; }
            yield return Bubble(result);
            yield return new WaitForSeconds(1f);
        }

        IEnumerator AnimScales(string result)
        {
            var pole = Mk(0, -60, 6, 160); pole.GetComponent<Image>().color = new Color(.45f,.35f,.2f);
            var beam = Mk(0, 50, 260, 5); beam.GetComponent<Image>().color = new Color(.55f,.45f,.2f);
            var lp = Mk(-100, -10, 70, 5); lp.GetComponent<Image>().color = new Color(.5f,.4f,.15f);
            var rp = Mk(100, -10, 70, 5); rp.GetComponent<Image>().color = new Color(.5f,.4f,.15f);
            float t = 0;
            while (t < 1.3f) {
                t += Time.deltaTime; float d = 1f-Mathf.Pow(t/1.3f,.6f);
                float a = Mathf.Sin(t*6f)*18f*d;
                beam.localRotation = Quaternion.Euler(0,0,a);
                float o = Mathf.Sin(t*6f)*35f*d;
                lp.anchoredPosition = new Vector2(-100, -10-o);
                rp.anchoredPosition = new Vector2(100, -10+o);
                yield return null;
            }
            beam.localRotation = Quaternion.Euler(0,0,-3);
            yield return Bubble(result);
            yield return new WaitForSeconds(1f);
        }

        IEnumerator AnimCarbon(string result)
        {
            var lines = new RectTransform[5];
            for (int i = 0; i < 5; i++) { lines[i] = Mk(0,0,4,600); lines[i].GetComponent<Image>().color = new Color(.25f,.8f,.25f,0); }
            var hud = Mk(-330, 280, 85, 28); hud.GetComponent<Image>().color = new Color(0,0,0,.6f);
            var htGo = new GameObject("ht", typeof(RectTransform));
            htGo.transform.SetParent(hud, false);
            var hrt = htGo.GetComponent<RectTransform>(); hrt.anchorMin=Vector2.zero; hrt.anchorMax=Vector2.one; hrt.offsetMin=hrt.offsetMax=Vector2.zero;
            var ht = htGo.AddComponent<Text>(); ht.text="C-14"; ht.font=Font.CreateDynamicFontFromOSFont("Arial",14);
            ht.fontSize=16; ht.color=new Color(.4f,.9f,.4f); ht.alignment=TextAnchor.MiddleCenter; ht.raycastTarget=false;
            animObjs.Add(htGo);
            float t = 0;
            while (t < 1f) {
                t += Time.deltaTime;
                for (int i = 0; i < 5; i++) {
                    float ph = (t*2.5f+i*.18f)%1f;
                    lines[i].anchoredPosition = new Vector2(Mathf.Lerp(-380,380,ph), 0);
                    lines[i].GetComponent<Image>().color = new Color(.25f,.8f,.25f, Mathf.Sin(ph*Mathf.PI)*.65f);
                }
                yield return null;
            }
            foreach (var l in lines) l.GetComponent<Image>().color = new Color(0,0,0,0);
            yield return Bubble(result);
            yield return new WaitForSeconds(1f);
        }

        IEnumerator AnimDict(string result)
        {
            var ring = Mk(-150, 0, 110, 110); ring.GetComponent<Image>().color = new Color(.3f,.25f,.15f,.85f);
            var inner = Mk(0, 0, 88, 88); inner.GetComponent<Image>().color = new Color(.9f,.9f,.85f,.3f);
            inner.SetParent(ring, false); inner.anchorMin=inner.anchorMax=new Vector2(.5f,.5f); inner.anchoredPosition=Vector2.zero;
            var handle = Mk(42, -42, 10, 50); handle.GetComponent<Image>().color = new Color(.4f,.3f,.15f);
            handle.SetParent(ring, false); handle.anchorMin=handle.anchorMax=new Vector2(.5f,.5f); handle.localRotation=Quaternion.Euler(0,0,-45);
            Vector2 s = new Vector2(-180,-40), c = new Vector2(0,80), e = new Vector2(140,40);
            float t = 0;
            while (t < .8f) {
                t += Time.deltaTime; float p = Mathf.SmoothStep(0,1,t/.8f);
                ring.anchoredPosition = Vector2.Lerp(Vector2.Lerp(s,c,p), Vector2.Lerp(c,e,p), p);
                ring.localScale = Vector3.one * (1f + .1f*Mathf.Sin(t*7f));
                yield return null;
            }
            ring.localScale = Vector3.one;
            inner.GetComponent<Image>().color = new Color(.8f,.95f,.8f,.5f);
            yield return new WaitForSeconds(.15f);
            yield return Bubble(result);
            yield return new WaitForSeconds(1f);
        }

        IEnumerator Bubble(string text)
        {
            var bg = Mk(0, -60, 230, 52); bg.GetComponent<Image>().color = PRI;
            var tGo = new GameObject("_bt", typeof(RectTransform));
            tGo.transform.SetParent(bg, false);
            var tr = tGo.GetComponent<RectTransform>(); tr.anchorMin=Vector2.zero; tr.anchorMax=Vector2.one; tr.offsetMin=tr.offsetMax=Vector2.zero;
            var tx = tGo.AddComponent<Text>(); tx.text=text; tx.font=Font.CreateDynamicFontFromOSFont("Arial",14);
            tx.fontSize=26; tx.color=Color.white; tx.alignment=TextAnchor.MiddleCenter; tx.raycastTarget=false;
            animObjs.Add(tGo);
            bg.localScale = Vector3.zero;
            float t = 0;
            while (t < .3f) { t += Time.deltaTime; float s = t < .2f ? t/.2f*1.15f : 1.15f-(t-.2f)/.1f*.15f; bg.localScale = Vector3.one*s; yield return null; }
            bg.localScale = Vector3.one;
        }

        RectTransform Mk(float x, float y, float w, float h)
        {
            var go = new GameObject("_a", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(animRoot, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(.5f,.5f);
            rt.pivot = new Vector2(.5f,.5f);
            rt.anchoredPosition = new Vector2(x,y);
            rt.sizeDelta = new Vector2(w,h);
            go.GetComponent<Image>().raycastTarget = false;
            animObjs.Add(go);
            return rt;
        }

        void ClearAnim() { foreach (var g in animObjs) if (g) Object.Destroy(g); animObjs.Clear(); }

        // ===== REFRESH =====
        void RefreshToolSlots()
        {
            for (int i = 0; i < 4 && i < gs.Config.tools.Count; i++)
            {
                var tool = gs.Config.tools[i];
                bool used = gs.UsedTools.Contains(tool.id);
                bool sel = gs.SelectedTool == tool.id;
                toolSlots[i].GetComponent<Image>().color = used
                    ? new Color(.4f,.35f,.25f)
                    : sel ? new Color(.6f,.5f,.3f) : new Color(.5f,.38f,.22f);
                if (used && gs.Art != null)
                    toolLabels[i].text = gs.Art.traits.Get(tool.reveals) + " \u2713";
                else
                    toolLabels[i].text = tool.name;
            }
        }

        void RefreshNote()
        {
            string[] ids = {"material","size","weight","year","textLanguage"};
            if (gs.Art == null) return;
            for (int i = 0; i < 5; i++)
            {
                bool rev = gs.Revealed.ContainsKey(ids[i]) && gs.Revealed[ids[i]];
                noteVal[i].text = rev ? gs.Art.traits.Get(ids[i]) : "\u2014";
                noteVal[i].color = rev ? new Color(.2f,.15f,.1f) : new Color(.7f,.65f,.55f);
            }
        }

        public void UpdateTimer()
        {
            if (!gs.TimerOn || timerT == null) return;
            int m = (int)(gs.TimeLeft/60), s = (int)(gs.TimeLeft%60);
            timerT.text = m + ":" + s.ToString("D2");
            timerT.color = gs.TimeLeft < 10 ? new Color(1f,.3f,.3f) : Color.white;
        }

        public override void Refresh()
        {
            var a = gs.Art; if (a == null) return;
            counterT.text = "Артефакт " + (gs.ArtIdx+1) + " / " + gs.Total;
            placedT.text = "Размещено: " + gs.PlacedN;
            sideIdx = 0;
            ShowSide(false);
            RefreshToolSlots();
            RefreshNote();
            drawerOpen = false;
            drawerRT.anchoredPosition = new Vector2(0, -460);
            gs.OnTrait -= OnTrait;
            gs.OnTrait += OnTrait;
        }

        void OnTrait(string t, string v) { RefreshNote(); RefreshToolSlots(); }
    }

    public class ToolCardDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public int toolIdx;
        public ExamineScreen examScreen;
        GameObject ghost;
        public void OnBeginDrag(PointerEventData e)
        {
            if (examScreen != null) examScreen.OnDragStart(toolIdx);
            ghost = new GameObject("Ghost", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            ghost.transform.SetParent(transform.root, false);
            ghost.transform.SetAsLastSibling();
            var img = ghost.GetComponent<Image>();
            var src = GetComponentInChildren<Image>();
            if (src != null && src.sprite != null) { img.sprite = src.sprite; img.preserveAspect = true; }
            else img.color = new Color(.4f,.3f,.2f,.6f);
            ghost.GetComponent<RectTransform>().sizeDelta = new Vector2(80, 80);
            ghost.GetComponent<CanvasGroup>().blocksRaycasts = false;
            ghost.GetComponent<CanvasGroup>().alpha = 0.8f;
            ghost.transform.position = e.position;
        }
        public void OnDrag(PointerEventData e) { if (ghost) ghost.transform.position = e.position; }
        public void OnEndDrag(PointerEventData e)
        {
            if (ghost) Destroy(ghost);
            if (examScreen != null) examScreen.OnDragEnd(toolIdx);
        }
    }

    public class ToolDropZone : MonoBehaviour, IDropHandler
    {
        public System.Action<string> onDrop;
        public void OnDrop(PointerEventData e)
        {
            var gs = GameState.I;
            if (gs != null && gs.SelectedTool != null) onDrop?.Invoke(gs.SelectedTool);
        }
    }
}
