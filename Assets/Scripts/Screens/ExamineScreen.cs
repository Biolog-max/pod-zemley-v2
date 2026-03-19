using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using static History.UIKit;

namespace History
{
    public class ExamineScreen : BaseScreen
    {
        Text timerT, counterT, placedT, sideNameT, zoneDescT;
        Image artImg; RectTransform artRT;
        int sideIdx;
        GameObject[] toolSlots = new GameObject[4];
        Text[] toolLabels = new Text[4];
        Text[] noteVal = new Text[5];
        Transform animRoot;
        List<GameObject> animObjs = new List<GameObject>();
        bool animating;

        protected override void Build(Transform p)
        {
            // ===== TOP BAR =====
            Img(p, 0, 80, Width, 55, new Color(0,0,0,.3f));
            timerT   = Txt(p, "1:00", Pad, 84, 150, 46, 50, Color.black);
            counterT = Txt(p, "", 0, 84, Width, 46, 30, Color.black, TextAnchor.MiddleCenter);
            placedT  = Txt(p, "", Width-280, 84, 240, 46, 36, Color.black, TextAnchor.MiddleRight);

            // ===== DESK =====
            Img(p, 0, 135, Width, 1785, new Color(.42f,.30f,.18f));
            Img(p, 8, 143, Width-16, 1769, new Color(.48f,.35f,.22f));

            // ===== ARTIFACT (big) =====
            Img(p, 70, 160, Width-140, 620, new Color(0,0,0,.15f)); // shadow
            artImg = SprImg(p, 60, 150, Width-120, 620, null);
            artRT = artImg.GetComponent<RectTransform>();
            artImg.GetComponent<Image>().raycastTarget = true;
            
            // anim layer
            var al = new GameObject("Anim", typeof(RectTransform));
            al.transform.SetParent(p, false); Pos(al, 60, 150, Width-120, 620);
            animRoot = al.transform;

            // drop zone
            var dz = Img(p, 50, 140, Width-100, 650, new Color(.35f,.55f,.35f,0));
            dz.raycastTarget = true;
            dz.gameObject.AddComponent<ToolDropZone>().onDrop = OnToolDropped;
            dz.gameObject.AddComponent<Button>().onClick.AddListener(OnArtTapped);

            // ===== SIDE NAV (explicit buttons) =====
            // Левая кнопка "Перевернуть"
            var leftBtn = Btn(p, "\u25C0 Перевернуть", 60, 775, 200, 60, 26, false, () => SwipeSide(-1));
            leftBtn.GetComponent<Image>().color = new Color(0.8f, 0.8f, 0.8f, 0.7f);

            // Центральный фон и текст
            // Увеличиваем ширину центрального фона до 240
            var centerBg = Img(p, Width / 2 - 120, 775, 240, 60, new Color(0.9f, 0.9f, 0.9f, 0.5f)); // ширина 240, центрирование X = Width/2 - 240/2
            // Обновляем рассчитанную X-позицию текста под новую ширину
            float calculatedTextX = (Width / 2.0f) - (240.0f / 2.0f);
            sideNameT = Txt(p, "", calculatedTextX, 775, 240, 60, 25, Color.black, TextAnchor.MiddleCenter); // ширина 240, высота 60, размер шрифта 25

            // Правая кнопка "Перевернуть"
            var rightBtn = Btn(p, "Перевернуть \u25B6", Width - 270, 775, 200, 60, 26, false, () => SwipeSide(1));
            rightBtn.GetComponent<Image>().color = new Color(0.8f, 0.8f, 0.8f, 0.7f);

            // zone description
            zoneDescT = Txt(p, "", Pad+5, 830, CW-10, 38, 25, new Color(.85f,.80f,.70f));

            // ===== TOOLS on desk =====
            float tw = (Width - Pad*2 - 30) / 4;
            string[] icons = gs.Config.tools.Count >= 4 ? new string[] {
                gs.Config.tools[0].icon, gs.Config.tools[1].icon,
                gs.Config.tools[2].icon, gs.Config.tools[3].icon } : null;
            for (int i = 0; i < 4; i++)
            {
                float tx = Pad + i*(tw+10);
                var slot = new GameObject("T"+i, typeof(RectTransform), typeof(Image));
                slot.transform.SetParent(p, false); Pos(slot, tx, 908, tw, 90);
                slot.GetComponent<Image>().color = new Color(.52f,.40f,.24f);
                toolSlots[i] = slot;
                if (icons != null) SprImg(slot.transform, (tw-44)/2, 5, 44, 44, icons[i]);
                // Увеличиваем размер шрифта и делаем цвет более контрастным (черный)
                toolLabels[i] = Txt(slot.transform, "", 0, 52, tw, 32, 25, Color.white, TextAnchor.MiddleCenter); // Было 13, стало 18; цвет .9f,.85f,.75f -> black
                var btn = slot.AddComponent<Button>();
                int idx = i;
                btn.onClick.AddListener(()=>OnToolTap(idx));
                var drag = slot.AddComponent<ToolCardDrag>();
                drag.toolIdx = i; drag.examScreen = this;
            }

            // ===== NOTEBOOK (always visible, compact) =====
            Img(p, Pad, 1040, CW, 210, new Color(.92f,.90f,.85f,.9f));
            string[] labels = {"Материал:","Размер:","Вес:","Год:","Язык:"};
            for (int i = 0; i < 5; i++)
            {
                float nx = Pad + (i < 3 ? 0 : CW/2);
                float ny = 1010 + (i < 3 ? i*42 : (i-3)*42);
                float nw = i < 3 ? CW/2 - 10 : CW/2 - 10;
                Txt(p, labels[i], nx+10, ny+50, 140, 35, 27, new Color(.5f,.45f,.35f));
                noteVal[i] = Txt(p, "\u2014", nx+155, ny+50, nw-165, 35, 30, new Color(.25f,.18f,.10f));
            }

            // ===== ACTION BUTTONS (explicit, always visible) =====
            var b1 = Btn(p, "Справочник", Pad, 0, CW/3-5, 80, 30, false, ()=>gs.OpenRef());
            PosBot(b1.gameObject, Pad, 60, CW/3-5, 80);
            var b2 = Btn(p, "Пропустить", Pad+CW/3+5, 0, CW/3-10, 80, 30, false, ()=>gs.SkipArt());
            PosBot(b2.gameObject, Pad+CW/3+5, 60, CW/3-10, 80);
            var b3 = Btn(p, "В МУЗЕЙ \u2192", Pad+CW*2/3+5, 0, CW/3-5, 80, 35, true, ()=>gs.GoMuseum());
            PosBot(b3.gameObject, Pad+CW*2/3+5, 60, CW/3-5, 80);

            gs.OnToolPicked += (_)=>RefreshTools();
        }

        // ===== SIDES =====
        void SwipeSide(int d)
        {
            if (gs.Art == null || gs.Art.zones.Count == 0) return;
            sideIdx = (sideIdx + d + gs.Art.zones.Count) % gs.Art.zones.Count;
            ShowSide(true);
        }
        void ShowSide(bool anim)
        {
            var a = gs.Art; if (a == null || a.zones.Count == 0) return;
            var z = a.zones[sideIdx];
            sideNameT.text = z.name;
            if (!string.IsNullOrEmpty(z.image))
            { var s = DataLoader.LoadSprite("Art/Artifacts/Sides/"+z.image); if (s!=null) { artImg.sprite=s; artImg.color=Color.white; } }
            gs.ExamZone(z.id);
            zoneDescT.text = gs.SeenZones.Contains(z.id) ? z.description : "";
            if (anim && root.activeInHierarchy) ((MonoBehaviour)gs).StartCoroutine(Flip());
        }
        IEnumerator Flip()
        {
            float t=0;
            while(t<.2f){t+=Time.deltaTime;float s=t<.1f?1f-t/.1f:(t-.1f)/.1f;artRT.localScale=new Vector3(s,1,1);yield return null;}
            artRT.localScale=Vector3.one;
        }

        // ===== TOOLS =====
        void OnToolTap(int i)
        {
            if (animating||gs.Config.tools.Count<=i) return;
            var tid=gs.Config.tools[i].id;
            if (gs.UsedTools.Contains(tid)) return;
            gs.SelectTool(tid);
            ((MonoBehaviour)gs).StartCoroutine(RunAnim(tid));
        }
        public void OnDragStart(int i)
        {
            if(gs.Config.tools.Count<=i)return;
            var tid=gs.Config.tools[i].id;
            if(!gs.UsedTools.Contains(tid)) gs.SelectTool(tid);
        }
        public void OnDragEnd(int i){}
        void OnToolDropped(string id)
        {
            if(animating||gs.SelectedTool==null)return;
            ((MonoBehaviour)gs).StartCoroutine(RunAnim(gs.SelectedTool));
        }
        void OnArtTapped()
        {
            if(animating||gs.SelectedTool==null)return;
            ((MonoBehaviour)gs).StartCoroutine(RunAnim(gs.SelectedTool));
        }

        IEnumerator RunAnim(string tid)
        {
            animating=true;
            var tool=gs.Config.tools.Find(x=>x.id==tid);
            if(tool==null){animating=false;yield break;}
            string res=gs.Art.traits.Get(tool.reveals);
            Clear();
            switch(tid)
            {
                case"ruler":yield return ARuler(res);break;
                case"scales":yield return AScales(res);break;
                case"carbon":yield return ACarbon(res);break;
                case"dictionary":yield return ADict(res);break;
            }
            gs.ApplyTool(); RefreshTools(); RefreshNote(); Clear();
            animating=false;
        }

        // ===== ANIMATIONS =====
        IEnumerator ARuler(string r)
        {
            var bar=M(0,0,0,14);bar.GetComponent<Image>().color=new Color(.6f,.4f,.2f,.9f);
            bar.pivot=new Vector2(0,.5f);bar.anchoredPosition=new Vector2(-380,0);
            float fw=760f,t=0;
            while(t<.5f){t+=Time.deltaTime;bar.sizeDelta=new Vector2(fw*t/.5f,14);yield return null;}
            for(int i=0;i<=8;i++){var tk=M(-380+fw*i/8f,18,3,i%2==0?28:14);tk.GetComponent<Image>().color=new Color(.35f,.2f,.1f);tk.pivot=new Vector2(.5f,0);}
            yield return new WaitForSeconds(.12f);
            var m1=M(-380+fw*.1f,28,5,32);m1.GetComponent<Image>().color=RED;
            var m2=M(-380+fw*.1f,28,5,32);m2.GetComponent<Image>().color=RED;
            t=0;while(t<.3f){t+=Time.deltaTime;m2.anchoredPosition=new Vector2(Mathf.Lerp(-380+fw*.1f,-380+fw*.85f,t/.3f),28);yield return null;}
            yield return Bub(r);yield return new WaitForSeconds(1f);
        }
        IEnumerator AScales(string r)
        {
            // --- 1. Создание заднего фона для весов ---

            // Основной фон (темный полупрозрачный прямоугольник)
            var bgPanel = M(0, 0, 500, 350);
            bgPanel.GetComponent<Image>().color = new Color(0.15f, 0.12f, 0.08f, 0f);

            // Рамка вокруг фона
            var bgBorder = M(0, 0, 510, 360);
            bgBorder.GetComponent<Image>().color = new Color(0.4f, 0.35f, 0.25f, 0f);

            // Декоративные углы фона
            var cornerTL = M(-245, 170, 20, 20);
            cornerTL.GetComponent<Image>().color = new Color(0.5f, 0.45f, 0.35f, 0f);
            var cornerTR = M(245, 170, 20, 20);
            cornerTR.GetComponent<Image>().color = new Color(0.5f, 0.45f, 0.35f, 0f);
            var cornerBL = M(-245, -170, 20, 20);
            cornerBL.GetComponent<Image>().color = new Color(0.5f, 0.45f, 0.35f, 0f);
            var cornerBR = M(245, -170, 20, 20);
            cornerBR.GetComponent<Image>().color = new Color(0.5f, 0.45f, 0.35f, 0f);

            // Анимация появления фона
            float bgAppearDuration = 0.4f;
            float t = 0f;
            while (t < bgAppearDuration)
            {
                t += Time.deltaTime;
                float progress = Mathf.SmoothStep(0f, 1f, t / bgAppearDuration);
                SetAlpha(bgPanel, progress * 0.85f);
                SetAlpha(bgBorder, progress * 0.6f);
                SetAlpha(cornerTL, progress * 0.7f);
                SetAlpha(cornerTR, progress * 0.7f);
                SetAlpha(cornerBL, progress * 0.7f);
                SetAlpha(cornerBR, progress * 0.7f);
                yield return null;
            }

            // Основание (подставка)
            var basePlate = M(0, -140, 60, 15);
            basePlate.GetComponent<Image>().color = new Color(.35f, .28f, .18f, 0f);

            // Стойка (вертикальная опора)
            var pole = M(0, -50, 10, 170);
            pole.GetComponent<Image>().color = new Color(.4f, .32f, .2f, 0f);

            // Декоративный элемент на стойке
            var poleDecor = M(0, 35, 16, 16);
            poleDecor.GetComponent<Image>().color = new Color(.5f, .4f, .25f, 0f);

            // Балка (коромысло)
            var beam = M(0, 45, 280, 10);
            beam.GetComponent<Image>().color = new Color(.65f, .55f, .35f, 0f);

            // Ось вращения (центральный элемент)
            var pivot = M(0, 45, 18, 18);
            pivot.GetComponent<Image>().color = new Color(.25f, .25f, .25f, 0f);

            // Цепи/подвесы для чаш
            var leftChain = M(-95, 20, 2, 30);
            leftChain.GetComponent<Image>().color = new Color(.5f, .45f, .35f, 0f);
            var rightChain = M(95, 20, 2, 30);
            rightChain.GetComponent<Image>().color = new Color(.5f, .45f, .35f, 0f);

            // Левая чаша
            var leftPan = M(-95, -5, 70, 12);
            leftPan.GetComponent<Image>().color = new Color(.55f, .45f, .25f, 0f);

            // Правая чаша
            var rightPan = M(95, -5, 70, 12);
            rightPan.GetComponent<Image>().color = new Color(.55f, .45f, .25f, 0f);

            // Тени под чашами
            var leftShadow = M(-95, -25, 60, 8);
            leftShadow.GetComponent<Image>().color = new Color(0, 0, 0, 0f);
            var rightShadow = M(95, -25, 60, 8);
            rightShadow.GetComponent<Image>().color = new Color(0, 0, 0, 0f);

            // --- 3. Анимация появления весов ---
            float appearDuration = 0.6f;
            t = 0f;

            while (t < appearDuration)
            {
                t += Time.deltaTime;
                float progress = Mathf.SmoothStep(0f, 1f, t / appearDuration);

                SetAlpha(basePlate, progress * 0.9f);
                SetAlpha(pole, progress * 0.9f);
                SetAlpha(poleDecor, progress * 0.8f);
                SetAlpha(beam, progress * 0.9f);
                SetAlpha(pivot, progress * 0.95f);
                SetAlpha(leftChain, progress * 0.85f);
                SetAlpha(rightChain, progress * 0.85f);
                SetAlpha(leftPan, progress * 0.85f);
                SetAlpha(rightPan, progress * 0.85f);
                SetAlpha(leftShadow, progress * 0.3f);
                SetAlpha(rightShadow, progress * 0.3f);

                yield return null;
            }

            // --- 4. Анимация взвешивания ---
            yield return new WaitForSeconds(0.3f);

            float scaleDuration = 2.0f;
            t = 0f;
            float oscillationSpeed = 5f;
            float initialAmplitude = 20f;
            float dampingFactor = 1.5f;
            float weightDifference = 0.25f;

            while (t < scaleDuration)
            {
                t += Time.deltaTime;
                float timeProgress = t / scaleDuration;
                float amplitude = initialAmplitude * Mathf.Exp(-dampingFactor * timeProgress);
                float baseAngle = -weightDifference * 18f;
                float oscillationAngle = Mathf.Sin(t * oscillationSpeed) * amplitude;
                float totalAngle = baseAngle + oscillationAngle;

                beam.localRotation = Quaternion.Euler(0, 0, totalAngle);

                float armLength = 95f;
                float angleRad = totalAngle * Mathf.Deg2Rad;
                float leftYOffset = -Mathf.Sin(angleRad) * armLength;
                float rightYOffset = Mathf.Sin(angleRad) * armLength;

                leftPan.anchoredPosition = new Vector2(-95, -5f + leftYOffset);
                rightPan.anchoredPosition = new Vector2(95, -5f + rightYOffset);
                leftChain.anchoredPosition = new Vector2(-95, 20f + leftYOffset * 0.5f);
                rightChain.anchoredPosition = new Vector2(95, 20f + rightYOffset * 0.5f);

                float leftShadowAlpha = 0.3f * (1f - Mathf.Abs(leftYOffset) / 30f);
                float rightShadowAlpha = 0.3f * (1f - Mathf.Abs(rightYOffset) / 30f);
                SetAlpha(leftShadow, Mathf.Max(0.1f, leftShadowAlpha));
                SetAlpha(rightShadow, Mathf.Max(0.1f, rightShadowAlpha));

                yield return null;
            }

            // --- 5. Финальная стабильная позиция ---
            float finalAngle = -weightDifference * 18f;
            beam.localRotation = Quaternion.Euler(0, 0, finalAngle);

            float finalAngleRad = finalAngle * Mathf.Deg2Rad;
            float finalLeftY = -Mathf.Sin(finalAngleRad) * 95f;
            float finalRightY = Mathf.Sin(finalAngleRad) * 95f;

            leftPan.anchoredPosition = new Vector2(-95, -5f + finalLeftY);
            rightPan.anchoredPosition = new Vector2(95, -5f + finalRightY);
            leftChain.anchoredPosition = new Vector2(-95, 20f + finalLeftY * 0.5f);
            rightChain.anchoredPosition = new Vector2(95, 20f + finalRightY * 0.5f);

            // --- 6. Визуальный эффект завершения ---
            float pulseDuration = 0.5f;
            t = 0f;
            while (t < pulseDuration)
            {
                t += Time.deltaTime;
                float pulse = 1f + Mathf.Sin(t * 12f) * 0.1f;
                pivot.localScale = new Vector3(pulse, pulse, 1);
                yield return null;
            }
            pivot.localScale = Vector3.one;

            // --- 7. Отображение результата ---
            yield return new WaitForSeconds(0.2f);
            yield return BubEnhanced(r);

            // --- 8. Плавное исчезновение всех элементов ---
            float fadeDuration = 0.5f;
            t = 0f;
            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                float alpha = Mathf.SmoothStep(1f, 0f, t / fadeDuration);

                SetAlpha(bgPanel, alpha * 0.85f);
                SetAlpha(bgBorder, alpha * 0.6f);
                SetAlpha(cornerTL, alpha * 0.7f);
                SetAlpha(cornerTR, alpha * 0.7f);
                SetAlpha(cornerBL, alpha * 0.7f);
                SetAlpha(cornerBR, alpha * 0.7f);
                SetAlpha(basePlate, alpha * 0.9f);
                SetAlpha(pole, alpha * 0.9f);
                SetAlpha(poleDecor, alpha * 0.8f);
                SetAlpha(beam, alpha * 0.9f);
                SetAlpha(pivot, alpha * 0.95f);
                SetAlpha(leftChain, alpha * 0.85f);
                SetAlpha(rightChain, alpha * 0.85f);
                SetAlpha(leftPan, alpha * 0.85f);
                SetAlpha(rightPan, alpha * 0.85f);
                SetAlpha(leftShadow, alpha * 0.3f);
                SetAlpha(rightShadow, alpha * 0.3f);

                yield return null;
            }

            // --- 9. Очистка ---
            Object.Destroy(bgPanel.gameObject);
            Object.Destroy(bgBorder.gameObject);
            Object.Destroy(cornerTL.gameObject);
            Object.Destroy(cornerTR.gameObject);
            Object.Destroy(cornerBL.gameObject);
            Object.Destroy(cornerBR.gameObject);
            Object.Destroy(basePlate.gameObject);
            Object.Destroy(pole.gameObject);
            Object.Destroy(poleDecor.gameObject);
            Object.Destroy(beam.gameObject);
            Object.Destroy(pivot.gameObject);
            Object.Destroy(leftChain.gameObject);
            Object.Destroy(rightChain.gameObject);
            Object.Destroy(leftPan.gameObject);
            Object.Destroy(rightPan.gameObject);
            Object.Destroy(leftShadow.gameObject);
            Object.Destroy(rightShadow.gameObject);

            yield return new WaitForSeconds(0.3f);
        }

        // Вспомогательная функция для установки прозрачности
        void SetAlpha(RectTransform rt, float alpha)
        {
            if (rt != null && rt.GetComponent<Image>() != null)
            {
                Color c = rt.GetComponent<Image>().color;
                rt.GetComponent<Image>().color = new Color(c.r, c.g, c.b, alpha);
            }
        }
        IEnumerator ACarbon(string r)
        {
            // Создаем 5 сканирующих линий с улучшенными параметрами
            var ls = new RectTransform[5];
            var glows = new RectTransform[5]; // Добавляем светодиоды для эффекта свечения

            for (int i = 0; i < 5; i++)
            {
                // Основная линия сканирования
                ls[i] = M(0, 0, 4, 550);
                ls[i].GetComponent<Image>().color = new Color(.1f, .6f, 1f, 0);

                // Слой свечения для линии
                glows[i] = M(0, 0, 12, 560);
                glows[i].GetComponent<Image>().color = new Color(.2f, .8f, 1.5f, 0);
            }

            // Добавляем частицы для эффекта распада
            List<GameObject> particles = new List<GameObject>();
            float t = 0;

            // Основной цикл анимации с улучшенными эффектами
            while (t < 1.2f)
            {
                t += Time.deltaTime;

                for (int i = 0; i < 5; i++)
                {
                    // Улучшенный расчет фазы с плавным движением
                    float ph = Mathf.SmoothStep(0, 1, (t * 2.2f + i * .15f) % 1f);

                    // Добавляем небольшие колебания по вертикали для имитации вибрации оборудования
                    float yOffset = Mathf.Sin(t * 15f + i) * 15f;

                    // Позиционируем основную линию
                    ls[i].anchoredPosition = new Vector2(Mathf.Lerp(-380, 380, ph), yOffset);

                    // Добавляем пульсацию яркости
                    float alpha = Mathf.Sin(ph * Mathf.PI) * .75f + Mathf.Sin(t * 10f) * .1f;
                    ls[i].GetComponent<Image>().color = new Color(.1f, .6f, 1f, alpha);

                    // Позиционируем и настраиваем свечение
                    glows[i].anchoredPosition = new Vector2(Mathf.Lerp(-380, 380, ph), yOffset);
                    float glowAlpha = alpha * 0.5f + 0.2f;
                    glows[i].GetComponent<Image>().color = new Color(.2f, .8f, 1.5f, glowAlpha);
                    glows[i].localScale = new Vector3(1, 1 + Mathf.Sin(t * 20f) * 0.2f, 1);

                    // Создаем частицы распада при движении линий
                    if (Time.frameCount % 3 == 0 && ph > 0.1f && ph < 0.9f)
                    {
                        float particleX = Mathf.Lerp(-380, 380, ph) + Random.Range(-10, 10);
                        float particleY = yOffset + Random.Range(-50, 50);

                        // Создаем частицу с эффектом распада
                        var particle = M(particleX, particleY, 3, 3);
                        particle.GetComponent<Image>().color = new Color(1f, 0.7f, 0.3f, 0.8f);

                        // Добавляем частицу в список для последующей анимации
                        particles.Add(particle.gameObject);

                        // Запускаем корутину анимации частицы
                        ((MonoBehaviour)gs).StartCoroutine(AnimateCarbonParticle(particle, 1.5f));
                    }
                }

                yield return null;
            }

            // Плавное исчезновение линий (без предварительного пульсирующего этапа)
            for (float fade = 1f; fade > 0; fade -= Time.deltaTime * 2f)
            {
                for (int i = 0; i < 5; i++)
                {
                    ls[i].GetComponent<Image>().color = new Color(.1f, .6f, 1f, fade * 0.6f);
                    glows[i].GetComponent<Image>().color = new Color(.2f, .8f, 1.5f, fade * 0.3f);
                }
                yield return null;
            }

            // Очистка временных объектов
            foreach (var l in ls)
                if (l != null) Object.Destroy(l.gameObject);

            foreach (var g in glows)
                if (g != null) Object.Destroy(g.gameObject);

            // Ожидание перед показом результата с эффектом "обработки данных"
            var processing = M(0, -50, 200, 30);
            processing.GetComponent<Image>().color = new Color(0, 0, 0, 0);

            var procText = new GameObject("ProcText", typeof(RectTransform), typeof(Text));
            procText.transform.SetParent(processing, false);
            var rt = procText.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = Vector2.one * 0.5f;
            rt.sizeDelta = new Vector2(180, 25);
            rt.anchoredPosition = Vector2.zero;

            var text = procText.GetComponent<Text>();
            text.font = Font.CreateDynamicFontFromOSFont("Arial", 18);
            text.color = new Color(0.2f, 0.8f, 1f);
            text.text = "ОБРАБОТКА ДАННЫХ...";
            text.alignment = TextAnchor.MiddleCenter;

            yield return new WaitForSeconds(0.3f);

            // Эффект мигающих точек в тексте
            for (int dots = 1; dots <= 3; dots++)
            {
                text.text = "ОБРАБОТКА ДАННЫХ" + new string('.', dots);
                yield return new WaitForSeconds(0.4f);
            }

            Object.Destroy(processing.gameObject);

            // Показываем результат с улучшенной анимацией
            yield return BubEnhanced(r);
            yield return new WaitForSeconds(1f);
        }

        // Вспомогательная корутина для анимации частиц распада
        IEnumerator AnimateCarbonParticle(RectTransform particle, float duration)
        {
            float t = 0;
            Vector2 startPos = particle.anchoredPosition;
            float startAlpha = particle.GetComponent<Image>().color.a;

            while (t < duration)
            {
                t += Time.deltaTime;

                // Частица движется вверх (имитация бета-частицы)
                float y = startPos.y + t * 200f;
                float x = startPos.x + Mathf.Sin(t * 5f) * 20f;

                // Уменьшается и становится прозрачнее
                float scale = 1f - t / duration;
                float alpha = startAlpha * (1f - t / duration);

                particle.anchoredPosition = new Vector2(x, y);
                particle.localScale = new Vector3(scale, scale, 1);
                particle.GetComponent<Image>().color = new Color(
                    particle.GetComponent<Image>().color.r,
                    particle.GetComponent<Image>().color.g,
                    particle.GetComponent<Image>().color.b,
                    alpha
                );

                yield return null;
            }

            if (particle != null) Object.Destroy(particle.gameObject);
        }

        // Улучшенная версия всплывающего окна с результатом
        IEnumerator BubEnhanced(string text)
        {
            var bg = M(0, -60, 260, 60);
            bg.GetComponent<Image>().color = new Color(0.05f, 0.2f, 0.4f, 0);

            // Добавляем эффект свечения вокруг пузыря
            var glow = M(0, -60, 280, 80);
            glow.GetComponent<Image>().color = new Color(0.1f, 0.4f, 0.8f, 0);

            var tGo = new GameObject("_bt", typeof(RectTransform));
            tGo.transform.SetParent(bg, false);
            var tr = tGo.GetComponent<RectTransform>();
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.offsetMin = tr.offsetMax = new Vector2(10, 10);

            var tx = tGo.AddComponent<Text>();
            tx.text = text;
            tx.font = Font.CreateDynamicFontFromOSFont("Arial", 28);
            tx.fontSize = 28;
            tx.color = Color.white;
            tx.alignment = TextAnchor.MiddleCenter;
            tx.raycastTarget = false;

            // Анимация появления с эффектом "расчета"
            float t = 0;
            while (t < 0.5f)
            {
                t += Time.deltaTime;
                float alpha = Mathf.SmoothStep(0, 1, t / 0.5f);
                float glowAlpha = alpha * 0.4f;
                float scale = 0.8f + Mathf.Sin(t * 10f) * 0.1f;

                bg.GetComponent<Image>().color = new Color(0.05f, 0.2f, 0.4f, alpha);
                glow.GetComponent<Image>().color = new Color(0.1f, 0.4f, 0.8f, glowAlpha);
                bg.localScale = new Vector3(scale, scale, 1);

                yield return null;
            }

            // Мигание итогового результата
            for (int i = 0; i < 2; i++)
            {
                tx.color = new Color(0.7f, 1f, 1f);
                yield return new WaitForSeconds(0.15f);
                tx.color = Color.white;
                yield return new WaitForSeconds(0.15f);
            }

            // Сохраняем финальное состояние
            bg.GetComponent<Image>().color = new Color(0.05f, 0.2f, 0.4f, 1);
            glow.GetComponent<Image>().color = new Color(0.1f, 0.4f, 0.8f, 0.4f);
            bg.localScale = Vector3.one;
        }
        IEnumerator ADict(string r)
        {
            // magnifying glass: ring + lens + handle
            var ring=M(-150,0,105,105);ring.GetComponent<Image>().color=new Color(.3f,.25f,.15f,.85f);
            var inn=M(0,0,84,84);inn.GetComponent<Image>().color=new Color(.9f,.9f,.85f,.3f);
            inn.SetParent(ring,false);inn.anchorMin=inn.anchorMax=new Vector2(.5f,.5f);inn.anchoredPosition=Vector2.zero;
            var hnd=M(40,-40,10,48);hnd.GetComponent<Image>().color=new Color(.4f,.3f,.15f);
            hnd.SetParent(ring,false);hnd.anchorMin=hnd.anchorMax=new Vector2(.5f,.5f);hnd.localRotation=Quaternion.Euler(0,0,-45);

            // fly along bezier
            Vector2 s=new Vector2(-170,-30),c=new Vector2(0,70),e=new Vector2(130,40);
            float t=0;while(t<.8f){t+=Time.deltaTime;float p=Mathf.SmoothStep(0,1,t/.8f);
            ring.anchoredPosition=Vector2.Lerp(Vector2.Lerp(s,c,p),Vector2.Lerp(c,e,p),p);
            ring.localScale=Vector3.one*(1f+.1f*Mathf.Sin(t*7f));yield return null;}
            ring.localScale=Vector3.one;

            // found text?
            bool hasText = gs.Art != null && !string.IsNullOrEmpty(gs.Art.traits.textLanguage);
            string inscPath = gs.Art != null ? gs.Art.inscription : null;

            if (hasText && !string.IsNullOrEmpty(inscPath))
            {
                // show inscription image inside lens area
                inn.GetComponent<Image>().color = Color.white;
                var spr = DataLoader.LoadSprite("Art/Artifacts/Inscriptions/" + inscPath);
                if (spr != null)
                {
                    inn.GetComponent<Image>().sprite = spr;
                    inn.GetComponent<Image>().preserveAspect = true;
                }

                // also show enlarged inscription panel below magnifier
                var panel = M(0, -120, 400, 180);
                panel.GetComponent<Image>().color = new Color(0,0,0,.75f);
                // inscription image in panel
                var inscGo = new GameObject("_insc", typeof(RectTransform), typeof(Image));
                inscGo.transform.SetParent(panel, false);
                var irt = inscGo.GetComponent<RectTransform>();
                irt.anchorMin = Vector2.zero; irt.anchorMax = Vector2.one;
                irt.offsetMin = new Vector2(10,10); irt.offsetMax = new Vector2(-10,-10);
                var iimg = inscGo.GetComponent<Image>();
                iimg.preserveAspect = true; iimg.raycastTarget = false;
                if (spr != null) { iimg.sprite = spr; iimg.color = Color.white; }
                animObjs.Add(inscGo);

                // fade in panel
                var cg = panel.gameObject.AddComponent<CanvasGroup>();
                cg.alpha = 0;
                float ft = 0;
                while (ft < .4f) { ft += Time.deltaTime; cg.alpha = ft / .4f; yield return null; }
                cg.alpha = 1;

                yield return new WaitForSeconds(1.5f);
            }
            else
            {
                // no text — lens stays translucent
                inn.GetComponent<Image>().color = new Color(.85f,.85f,.8f,.4f);
                yield return new WaitForSeconds(.3f);
            }

            yield return Bub(r);
            yield return new WaitForSeconds(1f);
        }
        IEnumerator Bub(string text)
        {
            var bg=M(0,-60,230,50);bg.GetComponent<Image>().color=PRI;
            var tGo=new GameObject("_bt",typeof(RectTransform));tGo.transform.SetParent(bg,false);
            var tr=tGo.GetComponent<RectTransform>();tr.anchorMin=Vector2.zero;tr.anchorMax=Vector2.one;tr.offsetMin=tr.offsetMax=Vector2.zero;
            var tx=tGo.AddComponent<Text>();tx.text=text;tx.font=Font.CreateDynamicFontFromOSFont("Arial",14);
            tx.fontSize=26;tx.color=Color.white;tx.alignment=TextAnchor.MiddleCenter;tx.raycastTarget=false;
            animObjs.Add(tGo);bg.localScale=Vector3.zero;float t=0;
            while(t<.3f){t+=Time.deltaTime;float s=t<.2f?t/.2f*1.15f:1.15f-(t-.2f)/.1f*.15f;bg.localScale=Vector3.one*s;yield return null;}
            bg.localScale=Vector3.one;
        }
        RectTransform M(float x,float y,float w,float h)
        {
            var go=new GameObject("_a",typeof(RectTransform),typeof(Image));
            go.transform.SetParent(animRoot,false);var rt=go.GetComponent<RectTransform>();
            rt.anchorMin=rt.anchorMax=new Vector2(.5f,.5f);rt.pivot=new Vector2(.5f,.5f);
            rt.anchoredPosition=new Vector2(x,y);rt.sizeDelta=new Vector2(w,h);
            go.GetComponent<Image>().raycastTarget=false;animObjs.Add(go);return rt;
        }
        void Clear(){foreach(var g in animObjs)if(g)Object.Destroy(g);animObjs.Clear();}

        // ===== REFRESH =====
        void RefreshTools()
        {
            for(int i=0;i<4&&i<gs.Config.tools.Count;i++)
            {
                var t=gs.Config.tools[i];bool used=gs.UsedTools.Contains(t.id);
                toolSlots[i].GetComponent<Image>().color=used?new Color(.4f,.35f,.25f):new Color(.52f,.40f,.24f);
                toolLabels[i].text=used&&gs.Art!=null?gs.Art.traits.Get(t.reveals)+" \u2713":t.name;
            }
        }
        void RefreshNote()
        {
            string[] ids={"material","size","weight","year","textLanguage"};
            if(gs.Art==null)return;
            for(int i=0;i<5;i++){bool rev=gs.Revealed.ContainsKey(ids[i])&&gs.Revealed[ids[i]];
            noteVal[i].text=rev?gs.Art.traits.Get(ids[i]):"\u2014";
            noteVal[i].color=rev?new Color(.25f,.18f,.10f):new Color(.7f,.65f,.55f);}
        }
        public void UpdateTimer()
        {
            if(!gs.TimerOn||timerT==null)return;
            int m=(int)(gs.TimeLeft/60),s=(int)(gs.TimeLeft%60);
            timerT.text=m+":"+s.ToString("D2");
            // Измените Color.white на Color.black
            timerT.color=gs.TimeLeft<10?new Color(1,.3f,.3f):Color.black; // Теперь по умолчанию черный
        }
        public override void Refresh()
        {
            if(gs.Art==null)return;
            counterT.text="Артефакт "+(gs.ArtIdx+1)+" / "+gs.Total;
            placedT.text="Размещено: "+gs.PlacedN;
            sideIdx=0;ShowSide(false);RefreshTools();RefreshNote();
            gs.OnTrait-=OT;gs.OnTrait+=OT;
        }
        void OT(string a,string b){RefreshNote();RefreshTools();}
    }

    public class ToolCardDrag:MonoBehaviour,IBeginDragHandler,IDragHandler,IEndDragHandler
    {
        public int toolIdx;public ExamineScreen examScreen;GameObject ghost;
        public void OnBeginDrag(PointerEventData e){
            if(examScreen!=null)examScreen.OnDragStart(toolIdx);
            ghost=new GameObject("G",typeof(RectTransform),typeof(Image),typeof(CanvasGroup));
            ghost.transform.SetParent(transform.root,false);ghost.transform.SetAsLastSibling();
            var img=ghost.GetComponent<Image>();var src=GetComponentInChildren<Image>();
            if(src!=null&&src.sprite!=null){img.sprite=src.sprite;img.preserveAspect=true;}else img.color=new Color(.4f,.3f,.2f,.6f);
            ghost.GetComponent<RectTransform>().sizeDelta=new Vector2(80,80);
            ghost.GetComponent<CanvasGroup>().blocksRaycasts=false;ghost.GetComponent<CanvasGroup>().alpha=.8f;
            ghost.transform.position=e.position;}
        public void OnDrag(PointerEventData e){if(ghost)ghost.transform.position=e.position;}
        public void OnEndDrag(PointerEventData e){if(ghost)Destroy(ghost);if(examScreen!=null)examScreen.OnDragEnd(toolIdx);}
    }

    public class ToolDropZone:MonoBehaviour,IDropHandler
    {
        public System.Action<string> onDrop;
        public void OnDrop(PointerEventData e){var g=GameState.I;if(g!=null&&g.SelectedTool!=null)onDrop?.Invoke(g.SelectedTool);}
    }
}
