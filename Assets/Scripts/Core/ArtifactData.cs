using System;
using System.Collections.Generic;

namespace History
{
    [Serializable] public class TraitSet
    {
        public string material = "";
        public float weight;
        public string size = "";
        public int year;
        public string textLanguage = "";

        public string Get(string id)
        {
            switch (id)
            {
                case "material": return material;
                case "weight": return weight > 0 ? weight + " g" : "";
                case "size": return size;
                case "year": return year > 0 ? "~" + year : "";
                case "textLanguage": return string.IsNullOrEmpty(textLanguage) ? "no text" : textLanguage;
                default: return "";
            }
        }
    }

    [Serializable] public class ZoneData
    {
        public string id, name, image, description;
        public List<string> revealsTraits = new List<string>();
    }

    [Serializable] public class ArtifactInfo
    {
        public string id, name, correctHall, correctWall, funFact;
        public TraitSet traits = new TraitSet();
        public List<ZoneData> zones = new List<ZoneData>();
    }

    [Serializable] public class HallData { public string id, name, description, banner; }
    [Serializable] public class WallData { public string id, name, description, icon; }
    [Serializable] public class ToolData { public string id, name, reveals, description, icon; }

    [Serializable] public class ScoringData
    {
        public int correctHall = 50, correctWall = 50, bothCorrectBonus = 100;
        public int allPlacedBonus = 50, allCorrectBonus = 200, speedBonus = 30, speedThresholdSec = 180;
    }

    [Serializable] public class ConfigData
    {
        public List<HallData> halls = new List<HallData>();
        public List<WallData> walls = new List<WallData>();
        public List<ToolData> tools = new List<ToolData>();
        public ScoringData scoring = new ScoringData();
    }

    [Serializable] public class RoundData
    {
        public string id, name, description;
        public int timeSec = 60, difficulty = 1;
        public List<ArtifactInfo> artifacts = new List<ArtifactInfo>();
    }

    public class Placement
    {
        public ArtifactInfo artifact;
        public string hall, wall;
        public bool hallOk, wallOk, skipped;
        public int points;
    }
}
