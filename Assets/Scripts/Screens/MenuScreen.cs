using UnityEngine;
using UnityEngine.UI;
using static History.UIKit;

namespace History
{
    public class MenuScreen : BaseScreen
    {
        Text timeTxt;
        int timeIdx = 1; // default 60s

        protected override void Build(Transform p)
        {
            Txt(p, "ПОД", 0, 340, Width, 100, 72, T1, TextAnchor.MiddleCenter);
            Txt(p, "ЗЕМЛЁЙ", 0, 430, Width, 100, 72, T1, TextAnchor.MiddleCenter);
            Img(p, Width/2 - 200, 535, 400, 2, DIV);
            Txt(p, "музейный хранитель", 0, 545, Width, 50, 26, T3, TextAnchor.MiddleCenter);

            float bx = (Width - 700) / 2;
            Btn(p, "Новая партия", bx, 680, 700, 100, 32, true, () => gs.NewRound());
            Btn(p, "Продолжить", bx, 810, 700, 100, 32, false, null).interactable = false;
            Btn(p, "Галерея", bx, 940, 700, 100, 32, false, null).interactable = false;
            Btn(p, "Настройки", bx, 1070, 700, 100, 32, false, null).interactable = false;

            Txt(p, "Время раунда:", 0, 1220, Width, 40, 22, T3, TextAnchor.MiddleCenter);
            Btn(p, "<", bx, 1270, 100, 80, 30, false, () => { timeIdx = Mathf.Max(0, timeIdx - 1); SyncTime(); });
            timeTxt = Txt(p, "60 сек", bx + 100, 1270, 500, 80, 28, T1, TextAnchor.MiddleCenter);
            Btn(p, ">", bx + 600, 1270, 100, 80, 30, false, () => { timeIdx = Mathf.Min(GameState.TimeOpts.Length - 1, timeIdx + 1); SyncTime(); });
            SyncTime();
        }

        void SyncTime()
        {
            int s = GameState.TimeOpts[timeIdx];
            gs.TimeOverride = s;
            timeTxt.text = s >= 60 ? (s / 60) + " мин" : s + " сек";
        }

        public override void Refresh() { }
    }
}
