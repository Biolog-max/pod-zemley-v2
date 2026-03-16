using UnityEngine;

namespace History
{
    public abstract class BaseScreen
    {
        protected GameObject root;
        protected GameState gs;

        public bool IsActive => root != null && root.activeSelf;

        public void Init(Transform parent, GameState state)
        {
            gs = state;
            root = UIKit.Panel(GetType().Name, parent);
            Build(root.transform);
        }

        public void Show() { root.SetActive(true); Refresh(); }
        public void Hide() { root.SetActive(false); }

        protected abstract void Build(Transform parent);
        public abstract void Refresh();
    }
}
