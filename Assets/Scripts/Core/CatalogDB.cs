using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace History
{
    public class CatalogDB
    {
        readonly List<ArtifactInfo> items;
        public CatalogDB(List<ArtifactInfo> catalog) { items = catalog; }

        public List<(ArtifactInfo item, int pct)> FindSimilar(ArtifactInfo a, Dictionary<string, bool> rev)
        {
            var res = new List<(ArtifactInfo, int)>();
            foreach (var e in items)
            {
                int m = 0, t = 0;
                if (R(rev, "material")) { t++; if (e.traits.material == a.traits.material) m++; }
                if (R(rev, "weight")) { t++; if (Mathf.Abs(e.traits.weight - a.traits.weight) / Mathf.Max(e.traits.weight, 1f) < 0.3f) m++; }
                if (R(rev, "size")) { t++; if (e.traits.size == a.traits.size) m++; }
                if (R(rev, "year")) { t++; if (Mathf.Abs(e.traits.year - a.traits.year) <= 20) m++; }
                if (R(rev, "textLanguage")) { t++; if (e.traits.textLanguage == a.traits.textLanguage) m++; }
                if (t == 0) { t = 1; if (e.traits.material == a.traits.material) m = 1; }
                res.Add((e, m * 100 / t));
            }
            return res.OrderByDescending(x => x.Item2).ToList();
        }

        static bool R(Dictionary<string, bool> d, string k) => d.ContainsKey(k) && d[k];
    }
}
