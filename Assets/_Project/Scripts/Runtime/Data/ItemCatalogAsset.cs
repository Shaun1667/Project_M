using System;
using System.Collections.Generic;
using ProjectM.Items;
using UnityEngine;

namespace ProjectM.Data
{
    /// <summary>
    /// 아이템 규칙 묶음 (Assets/_Project/Data/Items/ItemCatalog.asset, 3-2a). 기준은 BALANCE.md 9번.
    /// </summary>
    [CreateAssetMenu(menuName = "ProjectM/Item Catalog", fileName = "ItemCatalog")]
    public sealed class ItemCatalogAsset : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public string Id;
            [Tooltip("인벤토리 · 빠른 칸에 보이는 이름 (표시 전용). 비면 ID")] public string DisplayName;
            [Tooltip("사용하면 하는 일")] public ItemUse Use;
        }

        [SerializeField] private List<Entry> _items = new List<Entry>();

        public ItemBook ToBook()
        {
            var defs = new List<ItemDef>(_items.Count);
            foreach (var e in _items)
                defs.Add(new ItemDef(e.Id, e.Use));
            return new ItemBook(defs);
        }

        /// <summary>ID → 표시 이름 (이름이 빈 항목은 빠짐).</summary>
        public Dictionary<string, string> DisplayNames()
        {
            var names = new Dictionary<string, string>();
            foreach (var e in _items)
                if (!string.IsNullOrEmpty(e.Id) && !string.IsNullOrEmpty(e.DisplayName)) names[e.Id] = e.DisplayName;
            return names;
        }

        /// <summary>에디터 Setup용: 같은 ID가 없을 때만 추가한다.</summary>
        public bool AddIfMissing(Entry entry)
        {
            foreach (var e in _items)
                if (e.Id == entry.Id) return false;
            _items.Add(entry);
            return true;
        }
    }
}
