using UnityEngine;
using UnityEditor;
using History;
using System.IO;

public class TestHelper
{
    [MenuItem("Test/Screenshot")]
    static void Shot()
    {
        string dir = Path.Combine(Application.dataPath, "Screenshots");
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, "screen_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png");
        ScreenCapture.CaptureScreenshot(path, 1);
        Debug.Log("[T] Screenshot: " + path);
    }

    [MenuItem("Test/New Round")]
    static void NewRound() { var g = GameState.I; if (g != null) g.NewRound(); }

    [MenuItem("Test/Begin Exam")]
    static void Begin() { var g = GameState.I; if (g != null) g.BeginExam(); }

    [MenuItem("Test/Apply Ruler")]
    static void Ruler() { Apply("ruler"); }
    [MenuItem("Test/Apply Carbon")]
    static void Carbon() { Apply("carbon"); }
    [MenuItem("Test/Apply Scales")]
    static void Scales() { Apply("scales"); }
    [MenuItem("Test/Apply Dictionary")]
    static void Dict() { Apply("dictionary"); }

    static void Apply(string id)
    {
        var g = GameState.I;
        if (g == null) return;
        g.SelectTool(id);
        g.ApplyTool();
    }

    [MenuItem("Test/Go Museum")]
    static void Museum() { var g = GameState.I; if (g != null) g.GoMuseum(); }

    [MenuItem("Test/Place Hall 1")]
    static void Hall1() { var g = GameState.I; if (g != null) g.PickHall("hall_russia_19"); }

    [MenuItem("Test/Place Shelf Household")]
    static void ShelfH() { var g = GameState.I; if (g != null) g.PlaceOnShelf("household"); }

    [MenuItem("Test/Confirm")]
    static void Confirm() { var g = GameState.I; if (g != null) g.ConfirmPlace(); }

    [MenuItem("Test/Skip")]
    static void Skip() { var g = GameState.I; if (g != null) g.SkipArt(); }

    [MenuItem("Test/Next Art")]
    static void Next() { var g = GameState.I; if (g != null) g.NextArt(); }

    [MenuItem("Test/Print State")]
    static void State()
    {
        var g = GameState.I;
        if (g == null) { Debug.Log("[T] No GameState"); return; }
        Debug.Log("[T] Screen=" + g.Screen + " Art=" + (g.ArtIdx+1) + "/" + g.Total
            + " Placed=" + g.PlacedN + " Time=" + g.TimeLeft.ToString("F0") + "s"
            + " Note=" + g.NoteSummary());
    }

    [MenuItem("Test/Full Flow")]
    static void FullFlow()
    {
        var g = GameState.I;
        if (g == null) { Debug.Log("[T] No GameState"); return; }
        g.NewRound();
        g.BeginExam();
        Debug.Log("[T] === FULL FLOW ===");
        for (int i = 0; i < g.Total; i++)
        {
            var a = g.Art;
            if (a == null) break;
            Debug.Log("[T] Art " + (i+1) + ": " + a.name + " -> " + a.correctHall + "/" + a.correctWall);
            foreach (var t in g.Config.tools) { g.SelectTool(t.id); g.ApplyTool(); }
            foreach (var z in a.zones) g.ExamZone(z.id);
            g.PickHall(a.correctHall);
            g.PlaceOnShelf(a.correctWall);
            g.ConfirmPlace();
            g.NextArt();
        }
        Debug.Log("[T] Score: " + g.TotalScore() + " Rank: " + g.GetRank(g.TotalScore()));
    }
}
