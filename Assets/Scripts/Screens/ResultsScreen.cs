using UnityEngine;
using static History.UIKit;

namespace History
{
    public class ResultsScreen : BaseScreen
    {
        Transform listRoot;
        UnityEngine.UI.Text totalT, rankT, factT;

        protected override void Build(Transform p)
        {
            Txt(p, "Итоги раунда", 0, 80, Width, 55, 36, T1, TextAnchor.MiddleCenter);
            Img(p, Pad, 145, CW, 2, DIV);
            Txt(p, "АРТЕФАКТ", Pad + 10, 155, 500, 30, 18, T3);
            Txt(p, "ОЧКИ", Width - Pad - 100, 155, 90, 30, 18, T3, TextAnchor.MiddleRight);
            Img(p, Pad, 185, CW, 2, DIV);

            var lg = new GameObject("List", typeof(RectTransform));
            lg.transform.SetParent(p, false);
            Pos(lg, 0, 195, Width, 700);
            listRoot = lg.transform;

            Img(p, Pad, 950, CW, 2, DIV);
            Txt(p, "Итого:", Pad + 10, 970, 300, 45, 32, T1);
            totalT = Txt(p, "0", Width - Pad - 200, 970, 190, 45, 40, T1, TextAnchor.MiddleRight);

            Img(p, Pad, 1035, CW, 90, Cv(.95f));
            Txt(p, "Ранг:", 0, 1040, Width, 30, 20, T2, TextAnchor.MiddleCenter);
            rankT = Txt(p, "", 0, 1075, Width, 40, 30, T1, TextAnchor.MiddleCenter);

            Img(p, Pad, 1145, CW, 80, CARD);
            factT = Txt(p, "", Pad + 15, 1150, CW - 30, 70, 18, T2);

            var m1 = Btn(p, "В меню", Pad, 0, 480, 90, 24, false, () => gs.ToMenu());
            PosBot(m1.gameObject, Pad, 60, 480, 90);
            var m2 = Btn(p, "Ещё раунд", Pad + 520, 0, 480, 90, 24, true, () => gs.NewRound());
            PosBot(m2.gameObject, Pad + 520, 60, 480, 90);
        }

        public override void Refresh()
        {
            foreach (Transform c in listRoot) Object.Destroy(c.gameObject);
            float y = 0;
            foreach (var pl in gs.Placements)
            {
                Color col = pl.skipped ? T3 : T1;
                Txt(listRoot, pl.artifact.name, Pad + 10, y, 600, 28, 22, col);
                string st = pl.skipped ? "не размещён" :
                    "зал " + (pl.hallOk ? "\u2713" : "\u2717") + " полка " + (pl.wallOk ? "\u2713" : "\u2717");
                Txt(listRoot, st, Pad + 10, y + 30, 600, 22, 16, pl.skipped ? T3 : T2);
                Txt(listRoot, pl.skipped ? "0" : "+" + pl.points, Width - Pad - 130, y, 120, 55, 28, col, TextAnchor.MiddleRight);
                Img(listRoot, Pad, y + 58, CW, 1, DIV);
                y += 65;
            }

            int total = gs.TotalScore();
            int em = (int)(gs.Elapsed / 60), es = (int)(gs.Elapsed % 60);
            totalT.text = total + "  (" + em + ":" + es.ToString("D2") + ")";
            rankT.text = gs.GetRank(total);

            string fact = "";
            foreach (var pl in gs.Placements)
                if (!pl.skipped && !string.IsNullOrEmpty(pl.artifact.funFact)) { fact = pl.artifact.funFact; break; }
            factT.text = fact.Length > 0 ? "Факт: " + fact : "";
        }
    }
}
