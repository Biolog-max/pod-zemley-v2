using UnityEngine;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace History
{
    public static class DataLoader
    {
        public static ConfigData LoadConfig()
        {
            var t = Resources.Load<TextAsset>("Config/config");
            return t != null ? JsonConvert.DeserializeObject<ConfigData>(t.text) : new ConfigData();
        }

        public static List<ArtifactInfo> LoadCatalog()
        {
            var t = Resources.Load<TextAsset>("Config/catalog");
            return t != null ? JsonConvert.DeserializeObject<List<ArtifactInfo>>(t.text) : new List<ArtifactInfo>();
        }

        public static RoundData LoadRound(string id)
        {
            var t = Resources.Load<TextAsset>("Config/" + id);
            return t != null ? JsonConvert.DeserializeObject<RoundData>(t.text) : new RoundData();
        }

        public static Sprite LoadSprite(string path)
        {
            var tex = Resources.Load<Texture2D>(path);
            if (tex == null) return null;
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        }
    }
}
