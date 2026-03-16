using UnityEngine;
using UnityEngine.UI;
using static History.UIKit;

namespace History
{
    public class MuseumScreen : BaseScreen
    {
        Text titleT, hintT;
        Image[] hallBanners = new Image[3];
        Text[] hallNames = new Text[3];
        // shelf view
        GameObject hallSelectPanel, shelfPanel;
        Text shelfTitle;
        Text[] shelfNames = new Text[3];
        Text[] shelfCounts = new Text[3];
        Image artThumb;
        Text artInfo;
        string selectedHall;

        protected override void Build(Transform p)
        {
            // --- HALL SELECT VIEW ---
            hallSelectPanel = new GameObject("HallSelect", typeof(RectTransform));
            hallSelectPanel.transform.SetParent(p, false);
            var hrt = hallSelectPanel.GetComponent<RectTransform>();
            hrt.anchorMin = Vector2.zero; hrt.anchorMax = Vector2.one;
            hrt.offsetMin = hrt.offsetMax = Vector2.zero;
            var hp = hallSelectPanel.transform;

            Btn(hp, "< Осмотр", Pad, 80, 220, 88, 24, false, () => gs.BackExam());
            Txt(hp, "Выберите зал", 0, 80, Width, 88, 28, T1, TextAnchor.MiddleCenter);
            Img(hp, Pad, 190, CW, 70, Cv(.97f));
            hintT = Txt(hp, "", Pad + 15, 198, CW - 30, 54, 20, T2);
            Txt(hp, "Шаг 1: выберите зал", 0, 275, Width, 35, 20, T3, TextAnchor.MiddleCenter);

            for (int i = 0; i < 3; i++)
            {
                float cy = 330 + i * 200;
                hallBanners[i] = SprImg(hp, Pad, cy, CW, 80,
                    gs.Config.halls.Count > i ? gs.Config.halls[i].banner : null);
                Img(hp, Pad, cy + 80, CW, 100, WHITE);
                hallNames[i] = Txt(hp, "", Pad + 20, cy + 85, 900, 35, 26, T1);
                var desc = Txt(hp, "", Pad + 20, cy + 125, 900, 30, 18, T2);
                if (gs.Config.halls.Count > i) desc.text = gs.Config.halls[i].description;
                var ov = Img(hp, Pad, cy, CW, 180, new Color(0, 0, 0, 0));
                ov.raycastTarget = true;
                var btn = ov.gameObject.AddComponent<Button>();
                int idx = i;
                btn.onClick.AddListener(() => {
                    if (gs.Config.halls.Count > idx) SelectHall(gs.Config.halls[idx].id);
                });
            }

            // --- SHELF VIEW ---
            shelfPanel = new GameObject("ShelfView", typeof(RectTransform));
            shelfPanel.transform.SetParent(p, false);
            var srt = shelfPanel.GetComponent<RectTransform>();
            srt.anchorMin = Vector2.zero; srt.anchorMax = Vector2.one;
            srt.offsetMin = srt.offsetMax = Vector2.zero;
            shelfPanel.SetActive(false);
            var sp = shelfPanel.transform;

            Btn(sp, "< Залы", Pad, 80, 200, 88, 24, false, () => { shelfPanel.SetActive(false); hallSelectPanel.SetActive(true); });
            shelfTitle = Txt(sp, "", 0, 80, Width, 88, 28, T1, TextAnchor.MiddleCenter);
            Txt(sp, "Шаг 2: выберите полку", 0, 185, Width, 35, 20, T3, TextAnchor.MiddleCenter);

            for (int i = 0; i < 3; i++)
            {
                float sy = 240 + i * 180;
                // shelf background
                Img(sp, Pad, sy, CW, 160, WHITE);
                // shelf edge (wood line)
                Img(sp, Pad, sy + 130, CW, 8, new Color(.55f, .40f, .20f));
                // wall icon
                if (gs.Config.walls.Count > i)
                    SprImg(sp, Pad + 15, sy + 15, 60, 60, gs.Config.walls[i].icon);
                shelfNames[i] = Txt(sp, "", Pad + 90, sy + 15, 800, 35, 24, T1);
                if (gs.Config.walls.Count > i)
                    Txt(sp, gs.Config.walls[i].description, Pad + 90, sy + 50, 800, 25, 16, T3);
                shelfCounts[i] = Txt(sp, "", Pad + 90, sy + 80, 800, 25, 16, T3);
                // tap to place
                var ov = Img(sp, Pad, sy, CW, 160, new Color(0, 0, 0, 0));
                ov.raycastTarget = true;
                var btn = ov.gameObject.AddComponent<Button>();
                int idx = i;
                btn.onClick.AddListener(() => {
                    if (gs.Config.walls.Count > idx) gs.PlaceOnShelf(gs.Config.walls[idx].id);
                });
            }

            // artifact card at bottom
            Img(sp, Pad, 790, CW, 100, CARD);
            artThumb = SprImg(sp, Pad + 15, 800, 80, 80, null);
            artInfo = Txt(sp, "", Pad + 110, 800, 800, 80, 20, T1);
            Txt(sp, "Нажмите на полку для размещения", 0, 900, Width, 30, 18, T3, TextAnchor.MiddleCenter);
        }

        void SelectHall(string hallId)
        {
            selectedHall = hallId;
            gs.PickHall(hallId);
            hallSelectPanel.SetActive(false);
            shelfPanel.SetActive(true);
            RefreshShelves();
        }

        void RefreshShelves()
        {
            var h = gs.Hall(selectedHall);
            shelfTitle.text = h != null ? h.name : "";
            for (int i = 0; i < 3 && i < gs.Config.walls.Count; i++)
            {
                shelfNames[i].text = gs.Config.walls[i].name;
                int cnt = gs.Placements.FindAll(pl => !pl.skipped && pl.hall == selectedHall && pl.wall == gs.Config.walls[i].id).Count;
                shelfCounts[i].text = cnt > 0 ? "На полке: " + cnt : "Пусто";
            }
            // artifact card
            if (gs.Art != null)
            {
                artInfo.text = gs.Art.name + "\n" + gs.NoteSummary();
                var spr = DataLoader.LoadSprite("Art/Artifacts/Thumbs/" + gs.Art.id + "_thumb");
                if (spr != null) { artThumb.sprite = spr; artThumb.color = Color.white; }
            }
        }

        public override void Refresh()
        {
            hallSelectPanel.SetActive(true);
            shelfPanel.SetActive(false);
            hintT.text = "Артефакт " + (gs.ArtIdx + 1) + ": " + gs.NoteSummary();
            for (int i = 0; i < 3 && i < gs.Config.halls.Count; i++)
            {
                hallNames[i].text = gs.Config.halls[i].name;
            }
        }
    }
}
