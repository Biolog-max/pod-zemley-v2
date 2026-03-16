using UnityEngine;
using static History.UIKit;

namespace History
{
    public class TimeUpScreen : BaseScreen
    {
        protected override void Build(Transform p)
        {
            root.GetComponent<UnityEngine.UI.Image>().color = new Color(0, 0, 0, .4f);
            Img(p, (Width - 700) / 2, 500, 700, 420, WHITE);
            SprImg(p, Width / 2 - 48, 530, 96, 96, "Art/Icons/ic_clock");
            Txt(p, "Время вышло!", 0, 640, Width, 50, 36, T1, TextAnchor.MiddleCenter);
            Img(p, (Width - 600) / 2, 710, 600, 2, DIV);
            Txt(p, "Текущий артефакт\nне был размещён.", 0, 730, Width, 65, 22, T2, TextAnchor.MiddleCenter);
            Btn(p, "Результаты \u2192", (Width - 500) / 2, 830, 500, 80, 26, true, () => gs.ShowResults());
        }

        public override void Refresh() { }
    }
}
