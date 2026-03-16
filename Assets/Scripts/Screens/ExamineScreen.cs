using UnityEngine;
using UnityEngine.UI;
using static History.UIKit;

namespace History
{
    public class ExamineScreen : BaseScreen
    {
        Text timerT, counterT, placedT, artNameT, sideNameT, zoneDescT, hintT;
        Text[] noteVal = new Text[5];
        Button[] toolBtn = new Button[4];
        Image[] toolBg = new Image[4];
        Image artImg;
        int sideIdx;

        protected override void Build(Transform p)
        {
            // top bar
            Img(p, 0, 80, Width, 80, Cv(.95f));
            timerT   = Txt(p, "1:00", Pad, 85, 200, 70, 34, T1);
            counterT = Txt(p, "1/4", 0, 85, Width, 70, 24, T2, TextAnchor.MiddleCenter);
            placedT  = Txt(p, "0", Width - 350, 85, 310, 70, 20, T3, TextAnchor.MiddleRight);
            Img(p, Pad, 155, CW, 4, Cv(.9f));

            // artifact
            Img(p, Pad, 175, CW, 400, Cv(.95f));
            artImg = SprImg(p, Pad + 50, 185, CW - 100, 380, null);
            artNameT = Txt(p, "", 0, 540, Width, 30, 20, T3, TextAnchor.MiddleCenter);

            // side nav
            Btn(p, "\u25C0", Pad, 575, 60, 40, 22, false, () => SwipeSide(-1));
            sideNameT = Txt(p, "", 0, 575, Width, 40, 22, T1, TextAnchor.MiddleCenter);
            Btn(p, "\u25B6", Width - Pad - 60, 575, 60, 40, 22, false, () => SwipeSide(1));
            Txt(p, "\u2190 свайп \u2192", 0, 610, Width, 20, 14, T3, TextAnchor.MiddleCenter);

            zoneDescT = Txt(p, "", Pad + 10, 635, CW - 20, 45, 18, T2);
            hintT = Txt(p, "", 0, 635, Width, 40, 20, new Color(.53f,.43f,.2f), TextAnchor.MiddleCenter);
            hintT.gameObject.SetActive(false);

            // tools
            Img(p, 0, 685, Width, 2, DIV);
            Txt(p, "Инструменты:", Pad, 695, CW, 30, 22, T1);
            for (int i = 0; i < 4; i++)
            {
                float bx = Pad + i * 250;
                var b = Btn(p, "", bx, 730, 230, 75, 16, false, null);
                toolBtn[i] = b;
                toolBg[i] = b.GetComponent<Image>();
                if (gs.Config.tools.Count > i)
                    SprImg(b.transform, 5, 5, 35, 35, gs.Config.tools[i].icon);
                int idx = i;
                b.onClick.AddListener(() => {
                    if (gs.Config.tools.Count > idx) gs.SelectTool(gs.Config.tools[idx].id);
                });
            }

            // notebook
            Img(p, 0, 815, Width, 2, DIV);
            Txt(p, "Блокнот:", Pad, 825, CW, 30, 22, T1);
            Img(p, Pad, 860, CW, 255, CARD);
            string[] labels = { "Материал:", "Размер:", "Вес:", "Год:", "Язык:" };
            for (int i = 0; i < 5; i++)
            {
                float ny = 870 + i * 48;
                Txt(p, labels[i], Pad + 15, ny, 280, 35, 20, T3);
                noteVal[i] = Txt(p, "\u2014", Pad + 300, ny, 660, 35, 20, T1);
                if (i < 4) Img(p, Pad + 15, ny + 40, CW - 30, 1, DIV);
            }

            // actions
            Btn(p, "Справочник", Pad, 1140, CW, 75, 22, false, () => gs.OpenRef());
            Btn(p, "Пропустить", Pad, 1240, 480, 88, 22, false, () => gs.SkipArt());
            Btn(p, "В музей \u2192", Pad + 520, 1240, 480, 88, 26, true, () => gs.GoMuseum());

            // subscribe
            gs.OnToolPicked += OnToolPicked;
        }

        void SwipeSide(int dir)
        {
            if (gs.Art == null || gs.Art.zones.Count == 0) return;
            sideIdx = (sideIdx + dir + gs.Art.zones.Count) % gs.Art.zones.Count;
            ShowSide();
        }

        void ShowSide()
        {
            var a = gs.Art;
            if (a == null || a.zones.Count == 0) return;
            var z = a.zones[sideIdx];
            sideNameT.text = z.name;
            if (!string.IsNullOrEmpty(z.image))
            {
                var spr = DataLoader.LoadSprite("Art/Artifacts/Sides/" + z.image);
                if (spr != null) { artImg.sprite = spr; artImg.color = Color.white; }
            }
            gs.ExamZone(z.id);
            bool seen = gs.SeenZones.Contains(z.id);
            zoneDescT.text = seen ? z.description : "";
        }

        void OnToolPicked(string tid)
        {
            for (int i = 0; i < 4 && i < gs.Config.tools.Count; i++)
            {
                bool sel = tid != null && gs.Config.tools[i].id == tid;
                bool used = gs.UsedTools.Contains(gs.Config.tools[i].id);
                toolBg[i].color = used ? DIS : sel ? PRI : SEC;
            }
            hintT.gameObject.SetActive(tid != null);
            zoneDescT.gameObject.SetActive(tid == null);
            if (tid != null) hintT.text = "Нажмите на артефакт для замера";
        }

        public void UpdateTimer()
        {
            if (!gs.TimerOn || timerT == null) return;
            int m = (int)(gs.TimeLeft / 60), s = (int)(gs.TimeLeft % 60);
            timerT.text = m + ":" + s.ToString("D2");
            timerT.color = gs.TimeLeft < 10 ? RED : T1;
        }

        void RefreshNote()
        {
            string[] ids = { "material", "size", "weight", "year", "textLanguage" };
            var a = gs.Art;
            if (a == null) return;
            for (int i = 0; i < 5; i++)
            {
                bool rev = gs.Revealed.ContainsKey(ids[i]) && gs.Revealed[ids[i]];
                noteVal[i].text = rev ? a.traits.Get(ids[i]) : "\u2014";
                noteVal[i].color = rev ? T1 : T3;
            }
        }

        public override void Refresh()
        {
            var a = gs.Art;
            if (a == null) return;
            counterT.text = "Артефакт " + (gs.ArtIdx + 1) + " / " + gs.Total;
            placedT.text = "Размещено: " + gs.PlacedN;
            artNameT.text = a.name;
            sideIdx = 0;
            ShowSide();
            for (int i = 0; i < 4 && i < gs.Config.tools.Count; i++)
            {
                bool used = gs.UsedTools.Contains(gs.Config.tools[i].id);
                toolBg[i].color = used ? DIS : SEC;
                toolBtn[i].interactable = !used;
            }
            hintT.gameObject.SetActive(false);
            zoneDescT.gameObject.SetActive(true);
            RefreshNote();
            gs.OnTrait -= OnTraitReveal;
            gs.OnTrait += OnTraitReveal;
        }

        void OnTraitReveal(string t, string v) { RefreshNote(); }
    }
}
