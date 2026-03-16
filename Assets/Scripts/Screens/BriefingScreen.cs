using UnityEngine;
using UnityEngine.UI;
using static History.UIKit;

namespace History
{
    public class BriefingScreen : BaseScreen
    {
        Text nameT, descT, statsT;
        Text[] toolNm = new Text[4];
        Text[] hallNm = new Text[3];

        protected override void Build(Transform p)
        {
            Btn(p, "< Меню", Pad, 80, 200, 88, 24, false, () => gs.ToMenu());
            Txt(p, "Новая партия", 0, 80, Width, 88, 30, T1, TextAnchor.MiddleCenter);

            Img(p, Pad, 190, CW, 260, CARD);
            nameT  = Txt(p, "", 0, 210, Width, 50, 34, T1, TextAnchor.MiddleCenter);
            descT  = Txt(p, "", 80, 270, CW - 80, 100, 20, T2);
            statsT = Txt(p, "", 80, 390, CW - 80, 35, 20, T3);

            Txt(p, "Инструменты:", Pad, 480, CW, 35, 26, T1);
            for (int i = 0; i < 4; i++)
            {
                float tx = Pad + (i % 2) * 510, ty = 525 + (i / 2) * 110;
                Img(p, tx, ty, 490, 90, WHITE);
                if (gs.Config.tools.Count > i)
                    SprImg(p, tx + 10, ty + 10, 70, 70, gs.Config.tools[i].icon);
                toolNm[i] = Txt(p, "", tx + 90, ty + 15, 380, 60, 20, T1);
            }

            Txt(p, "Залы музея:", Pad, 770, CW, 35, 26, T1);
            for (int i = 0; i < 3; i++)
            {
                float hx = Pad + i * 340;
                if (gs.Config.halls.Count > i)
                    SprImg(p, hx, 815, 320, 90, gs.Config.halls[i].banner);
                hallNm[i] = Txt(p, "", hx + 10, 820, 300, 80, 18, WHITE, TextAnchor.MiddleCenter);
            }

            Txt(p, "В каждом зале 3 полки: оружие · быт · документы", Pad, 920, CW, 30, 18, T3);

            var sb = Btn(p, "НАЧАТЬ РАУНД", Pad, 0, CW, 100, 32, true, () => gs.BeginExam());
            PosBot(sb.gameObject, Pad, 60, CW, 100);
        }

        public override void Refresh()
        {
            var r = gs.Round;
            if (r == null) return;
            nameT.text = r.name;
            descT.text = r.description;
            int t = gs.TimeOverride > 0 ? gs.TimeOverride : r.timeSec;
            statsT.text = "Артефактов: " + r.artifacts.Count + "  ·  " + (t >= 60 ? t/60 + " мин" : t + " сек");
            for (int i = 0; i < 4 && i < gs.Config.tools.Count; i++)
                toolNm[i].text = gs.Config.tools[i].name + "\n" + gs.Config.tools[i].description;
            for (int i = 0; i < 3 && i < gs.Config.halls.Count; i++)
                hallNm[i].text = gs.Config.halls[i].name;
        }
    }
}
