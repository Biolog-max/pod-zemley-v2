using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace History
{
    public class ScreenManager : MonoBehaviour
    {
        Dictionary<GScreen, BaseScreen> screens = new Dictionary<GScreen, BaseScreen>();
        GameState gs;

        void Start()
        {
            gs = GameState.I;

            // Canvas
            var cgo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var cv = cgo.GetComponent<Canvas>();
            cv.renderMode = RenderMode.ScreenSpaceOverlay;
            cv.sortingOrder = 100;
            var sc = cgo.GetComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            sc.referenceResolution = new Vector2(1080, 1920);
            sc.matchWidthOrHeight = 0f;
            sc.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;

            // EventSystem
            if (FindFirstObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            var root = cgo.transform;

            // Register screens
            Register(GScreen.Menu, new MenuScreen(), root);
            Register(GScreen.Briefing, new BriefingScreen(), root);
            Register(GScreen.Examine, new ExamineScreen(), root);
            Register(GScreen.Reference, new ReferenceScreen(), root);
            Register(GScreen.Museum, new MuseumScreen(), root);
            Register(GScreen.Confirm, new ConfirmScreen(), root);
            Register(GScreen.Placed, new PlacedScreen(), root);
            Register(GScreen.Results, new ResultsScreen(), root);
            Register(GScreen.TimeUp, new TimeUpScreen(), root);

            gs.OnScreen += OnScreen;
            OnScreen(GScreen.Menu);
        }

        void Register(GScreen id, BaseScreen screen, Transform root)
        {
            screen.Init(root, gs);
            screens[id] = screen;
        }

        void OnScreen(GScreen s)
        {
            foreach (var kv in screens) kv.Value.Hide();
            if (screens.ContainsKey(s)) screens[s].Show();
            if (s == GScreen.Placed) StartCoroutine(AutoNext());
        }

        IEnumerator AutoNext()
        {
            yield return new WaitForSeconds(2f);
            if (gs.Screen == GScreen.Placed) gs.NextArt();
        }

        void Update()
        {
            // update timer on active examine screen
            if (screens.ContainsKey(GScreen.Examine) && screens[GScreen.Examine] is ExamineScreen ex)
                ex.UpdateTimer();
        }
    }
}
