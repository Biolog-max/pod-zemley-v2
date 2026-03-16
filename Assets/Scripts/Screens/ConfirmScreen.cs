using UnityEngine;
using static History.UIKit;

namespace History
{
    public class ConfirmScreen : BaseScreen
    {
        UnityEngine.UI.Text artT, hallT, wallT;

        protected override void Build(Transform p)
        {
            root.GetComponent<UnityEngine.UI.Image>().color = new Color(0, 0, 0, .4f);
            float mx = (Width - 800) / 2;
            Img(p, mx, 450, 800, 500, WHITE);
            Txt(p, "Подтвердите", 0, 480, Width, 50, 32, T1, TextAnchor.MiddleCenter);
            Img(p, mx + 40, 540, 720, 2, DIV);
            Txt(p, "Артефакт:", mx + 50, 560, 700, 30, 20, T3);
            artT = Txt(p, "", mx + 50, 590, 700, 35, 24, T1);
            Img(p, mx + 40, 640, 720, 2, DIV);
            Txt(p, "Размещение:", mx + 50, 660, 700, 30, 20, T3);
            hallT = Txt(p, "", mx + 50, 690, 700, 35, 24, T1);
            wallT = Txt(p, "", mx + 50, 725, 700, 35, 24, T1);
            Img(p, mx + 40, 775, 720, 2, DIV);
            Img(p, mx + 60, 790, 680, 30, new Color(1f, .95f, .9f));
            Txt(p, "Переставить нельзя!", 0, 792, Width, 26, 18, RED, TextAnchor.MiddleCenter);
            Btn(p, "Отмена", mx + 40, 840, 330, 80, 24, false, () => gs.CancelPlace());
            Btn(p, "Да", mx + 430, 840, 330, 80, 24, true, () => gs.ConfirmPlace());
        }

        public override void Refresh()
        {
            artT.text = gs.NoteSummary();
            var h = gs.Hall(gs.PickedHall);
            var w = gs.Wall(gs.PickedWall);
            hallT.text = h != null ? h.name : "";
            wallT.text = w != null ? w.name : "";
        }
    }
}
