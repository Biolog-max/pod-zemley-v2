using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static History.UIKit;

namespace History
{
    public class ReferenceScreen : BaseScreen
    {
        int page;
        List<(ArtifactInfo, int)> data;
        Text pageTxt;
        GameObject[] cards = new GameObject[3];
        Text[] nm = new Text[3], tr = new Text[3], hw = new Text[3], pct = new Text[3];
        Image[] thumbs = new Image[3];

        protected override void Build(Transform p)
        {
            Btn(p, "< Осмотр", Pad, 80, 220, 88, 24, false, () => gs.CloseRef());
            Txt(p, "Справочник", 0, 80, Width, 88, 30, T1, TextAnchor.MiddleCenter);
            Txt(p, "Похожие на ваш артефакт:", Pad, 185, CW, 30, 20, T3);

            for (int i = 0; i < 3; i++)
            {
                float cy = 230 + i * 340;
                var card = new GameObject("C" + i, typeof(RectTransform), typeof(Image));
                card.transform.SetParent(p, false);
                Pos(card, Pad, cy, CW, 320);
                card.GetComponent<Image>().color = CARD;
                card.GetComponent<Image>().raycastTarget = false;
                cards[i] = card;
                thumbs[i] = SprImg(card.transform, 15, 15, 140, 140, null);
                nm[i]  = Txt(card.transform, "", 170, 10, 800, 30, 24, T1);
                tr[i]  = Txt(card.transform, "", 170, 45, 800, 70, 18, T2);
                Img(card.transform, 170, 150, 720, 1, DIV);
                hw[i]  = Txt(card.transform, "", 170, 160, 800, 50, 18, T2);
                pct[i] = Txt(card.transform, "", 170, 260, 800, 30, 20, ACCENT, TextAnchor.MiddleRight);
            }

            Btn(p, "<", Pad, 1280, 120, 88, 28, false, () => { if (page > 0) { page--; Fill(); } });
            pageTxt = Txt(p, "1/1", 0, 1285, Width, 80, 22, T2, TextAnchor.MiddleCenter);
            Btn(p, ">", Width - Pad - 120, 1280, 120, 88, 28, false, () => { page++; Fill(); });

            var bb = Btn(p, "< Вернуться к осмотру", Pad, 0, CW, 88, 22, false, () => gs.CloseRef());
            PosBot(bb.gameObject, Pad, 60, CW, 88);
        }

        public override void Refresh() { page = 0; Fill(); }

        void Fill()
        {
            data = gs.CatalogDB.FindSimilar(gs.Art, gs.Revealed);
            int tp = Mathf.Max(1, Mathf.CeilToInt(data.Count / 3f));
            page = Mathf.Clamp(page, 0, tp - 1);
            pageTxt.text = (page + 1) + " / " + tp;
            for (int i = 0; i < 3; i++)
            {
                int di = page * 3 + i;
                bool show = di < data.Count;
                cards[i].SetActive(show);
                if (!show) continue;
                var (e, pc) = data[di];
                nm[i].text = e.name;
                tr[i].text = e.traits.material + " · " + e.traits.weight + "g · " + e.traits.size + " · ~" + e.traits.year;
                var h = gs.Hall(e.correctHall);
                var w = gs.Wall(e.correctWall);
                hw[i].text = "Зал: " + (h != null ? h.name : "?") + "\nПолка: " + (w != null ? w.name : "?");
                pct[i].text = "Совпадение: " + pc + "%";
                var spr = DataLoader.LoadSprite("Art/Artifacts/Thumbs/" + e.id + "_thumb");
                if (spr != null) { thumbs[i].sprite = spr; thumbs[i].color = Color.white; }
            }
        }
    }
}
