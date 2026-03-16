using UnityEngine;
using static History.UIKit;

namespace History
{
    public class PlacedScreen : BaseScreen
    {
        UnityEngine.UI.Text titleT, infoT, remainT, timeT;

        protected override void Build(Transform p)
        {
            SprImg(p, Width / 2 - 64, 420, 128, 128, "Art/Icons/ic_checkmark");
            titleT  = Txt(p, "Размещён!", 0, 570, Width, 50, 36, T1, TextAnchor.MiddleCenter);
            infoT   = Txt(p, "", 0, 640, Width, 35, 22, T2, TextAnchor.MiddleCenter);
            Img(p, Width / 2 - 200, 700, 400, 2, DIV);
            remainT = Txt(p, "", 0, 730, Width, 35, 24, T3, TextAnchor.MiddleCenter);
            timeT   = Txt(p, "", 0, 775, Width, 35, 24, T3, TextAnchor.MiddleCenter);
            Txt(p, "Достаём следующий артефакт...", 0, 860, Width, 30, 20, T3, TextAnchor.MiddleCenter);
        }

        public override void Refresh()
        {
            var last = gs.Placements.Count > 0 ? gs.Placements[gs.Placements.Count - 1] : null;
            if (last == null) return;
            if (last.skipped) { titleT.text = "Пропущен"; infoT.text = last.artifact.name; }
            else
            {
                titleT.text = "Размещён!";
                var h = gs.Hall(last.hall);
                var w = gs.Wall(last.wall);
                infoT.text = (h != null ? h.name : "") + " · " + (w != null ? w.name : "");
            }
            remainT.text = "Осталось: " + gs.Remain;
            int m = (int)(gs.TimeLeft / 60), s = (int)(gs.TimeLeft % 60);
            timeT.text = "Время: " + m + ":" + s.ToString("D2");
        }
    }
}
