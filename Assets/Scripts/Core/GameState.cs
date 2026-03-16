using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace History
{
    public enum GScreen { Menu, Briefing, Examine, Reference, Museum, Confirm, Placed, Results, TimeUp }

    public class GameState : MonoBehaviour
    {
        public static GameState I { get; private set; }
        public ConfigData Config;
        public List<ArtifactInfo> Catalog;
        public CatalogDB CatalogDB;

        public GScreen Screen { get; private set; } = GScreen.Menu;
        public RoundData Round;
        public int ArtIdx;
        public ArtifactInfo Art => Round != null && ArtIdx < Round.artifacts.Count ? Round.artifacts[ArtIdx] : null;

        public float TimeLeft, Elapsed;
        public bool TimerOn;
        public int TimeOverride;
        public static readonly int[] TimeOpts = { 30, 60, 90, 120, 180, 300 };

        public Dictionary<string, bool> Revealed = new Dictionary<string, bool>();
        public HashSet<string> UsedTools = new HashSet<string>();
        public HashSet<string> SeenZones = new HashSet<string>();
        public string PickedHall, PickedWall, SelectedTool;
        public List<Placement> Placements = new List<Placement>();

        public int PlacedN => Placements.Count(p => !p.skipped);
        public int Total => Round != null ? Round.artifacts.Count : 0;
        public int Remain => Total - Placements.Count;

        public event Action<GScreen> OnScreen;
        public event Action<string, string> OnTrait;
        public event Action<ZoneData> OnZone;
        public event Action<string> OnToolPicked;
        public event Action<string, string> OnToolUsed;

        void Awake()
        {
            if (I != null) { Destroy(gameObject); return; }
            I = this;
            Application.targetFrameRate = 60;
            UnityEngine.Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Config = DataLoader.LoadConfig();
            Catalog = DataLoader.LoadCatalog();
            CatalogDB = new CatalogDB(Catalog);
        }

        void Update()
        {
            if (!TimerOn || TimeLeft <= 0) return;
            TimeLeft -= Time.deltaTime;
            Elapsed += Time.deltaTime;
            if (TimeLeft <= 0) { TimeLeft = 0; TimerOn = false; Go(GScreen.TimeUp); }
        }

        public void Go(GScreen s) { Screen = s; OnScreen?.Invoke(s); }

        public void NewRound(string id = "round_01")
        {
            Round = DataLoader.LoadRound(id);
            Placements.Clear(); ArtIdx = 0;
            Revealed.Clear(); UsedTools.Clear(); SeenZones.Clear();
            SelectedTool = null; PickedHall = PickedWall = null;
            TimeLeft = TimeOverride > 0 ? TimeOverride : Round.timeSec;
            Elapsed = 0; TimerOn = false;
            Go(GScreen.Briefing);
        }

        public void BeginExam() { TimerOn = true; PrepArt(); Go(GScreen.Examine); }

        void PrepArt()
        {
            Revealed.Clear(); UsedTools.Clear(); SeenZones.Clear();
            PickedHall = PickedWall = null; SelectedTool = null;
            if (Art != null) Revealed["material"] = true;
        }

        public void ExamZone(string zid)
        {
            if (Art == null) return;
            var z = Art.zones.Find(x => x.id == zid);
            if (z == null) return;
            SeenZones.Add(zid);
            foreach (var tr in z.revealsTraits)
                if (!Revealed.ContainsKey(tr) || !Revealed[tr])
                { Revealed[tr] = true; OnTrait?.Invoke(tr, Art.traits.Get(tr)); }
            OnZone?.Invoke(z);
        }

        public void SelectTool(string tid)
        {
            if (UsedTools.Contains(tid)) return;
            SelectedTool = SelectedTool == tid ? null : tid;
            OnToolPicked?.Invoke(SelectedTool);
        }

        public void ApplyTool()
        {
            if (Art == null || SelectedTool == null) return;
            var t = Config.tools.Find(x => x.id == SelectedTool);
            if (t == null || UsedTools.Contains(SelectedTool)) return;
            UsedTools.Add(SelectedTool);
            Revealed[t.reveals] = true;
            string val = Art.traits.Get(t.reveals);
            OnTrait?.Invoke(t.reveals, val);
            OnToolUsed?.Invoke(SelectedTool, val);
            SelectedTool = null; OnToolPicked?.Invoke(null);
        }

        public void GoMuseum() { Go(GScreen.Museum); }
        public void OpenRef() { Go(GScreen.Reference); }
        public void CloseRef() { Go(GScreen.Examine); }
        public void PickHall(string id) { PickedHall = id; }
        public void BackExam() { Go(GScreen.Examine); }

        public void PlaceOnShelf(string wallId)
        {
            PickedWall = wallId;
            Go(GScreen.Confirm);
        }

        public void CancelPlace() { Go(GScreen.Museum); }

        public void ConfirmPlace()
        {
            if (Art == null) return;
            bool h = PickedHall == Art.correctHall, w = PickedWall == Art.correctWall;
            int pts = (h ? Config.scoring.correctHall : 0) + (w ? Config.scoring.correctWall : 0)
                    + (h && w ? Config.scoring.bothCorrectBonus : 0);
            Placements.Add(new Placement {
                artifact = Art, hall = PickedHall, wall = PickedWall,
                hallOk = h, wallOk = w, points = pts });
            Go(GScreen.Placed);
        }

        public void SkipArt()
        {
            if (Art == null) return;
            Placements.Add(new Placement { artifact = Art, skipped = true });
            Go(GScreen.Placed);
        }

        public void NextArt()
        {
            ArtIdx++;
            if (ArtIdx >= Total) { TimerOn = false; Go(GScreen.Results); return; }
            PrepArt(); Go(GScreen.Examine);
        }

        public void ShowResults() { TimerOn = false; Go(GScreen.Results); }
        public void ToMenu() { Go(GScreen.Menu); }

        public int TotalScore()
        {
            int s = Placements.Sum(p => p.points);
            if (PlacedN == Total) s += Config.scoring.allPlacedBonus;
            if (Placements.All(p => !p.skipped && p.hallOk && p.wallOk)) s += Config.scoring.allCorrectBonus;
            if (Elapsed < Config.scoring.speedThresholdSec && PlacedN == Total) s += Config.scoring.speedBonus;
            return s;
        }

        public string GetRank(int s) =>
            s >= 800 ? "Директор музея" : s >= 500 ? "Ст. хранитель" :
            s >= 300 ? "Хранитель" : s >= 100 ? "Стажёр" : "Практикант";

        public HallData Hall(string id) => Config.halls.Find(h => h.id == id);
        public WallData Wall(string id) => Config.walls.Find(w => w.id == id);
        public int InHall(string hid) => Placements.Count(p => !p.skipped && p.hall == hid);

        public string NoteSummary()
        {
            if (Art == null) return "";
            var p = new List<string>();
            if (Revealed.ContainsKey("material") && Revealed["material"]) p.Add(Art.traits.material);
            if (Revealed.ContainsKey("year") && Revealed["year"]) p.Add("~" + Art.traits.year);
            if (Revealed.ContainsKey("textLanguage") && Revealed["textLanguage"] && !string.IsNullOrEmpty(Art.traits.textLanguage))
                p.Add(Art.traits.textLanguage);
            return string.Join(" | ", p);
        }
    }
}
