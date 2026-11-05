using System;
using System.Collections.Generic;
using ProjectM.Core;
using ProjectM.Dungeon;
using ProjectM.Units;

namespace ProjectM.Items
{
    /// <summary>마법 제작대 수치 (3-28b — BALANCE 24번). 값은 FloorDecorConfigAsset.ToWorkbench().</summary>
    public sealed class WorkbenchConfig
    {
        /// <summary>유물 하나 만들기 비용 (에테르).</summary>
        public int CraftCost { get; }
        /// <summary>유물 하나 분해로 얻는 에테르 (강화 단계와 무관).</summary>
        public int DisassembleGain { get; }
        /// <summary>강화 비용 — [0] = +1, [1] = +2, … 길이 = 최대 강화 단계.</summary>
        public IReadOnlyList<int> UpgradeCosts { get; }
        /// <summary>작업실 방 안쪽 크기 (정사각 한 변).</summary>
        public int RoomMinSize { get; }
        public int RoomMaxSize { get; }

        public int MaxUpgrade => UpgradeCosts.Count;

        public WorkbenchConfig(int craftCost, int disassembleGain, IReadOnlyList<int> upgradeCosts, int roomMinSize = 3, int roomMaxSize = 4)
        {
            if (craftCost < 0 || disassembleGain < 0) throw new ArgumentOutOfRangeException(nameof(craftCost));
            if (upgradeCosts == null) throw new ArgumentNullException(nameof(upgradeCosts));
            foreach (var c in upgradeCosts) if (c < 0) throw new ArgumentOutOfRangeException(nameof(upgradeCosts));
            if (roomMinSize < 2 || roomMaxSize < roomMinSize) throw new ArgumentOutOfRangeException(nameof(roomMaxSize));
            CraftCost = craftCost;
            DisassembleGain = disassembleGain;
            UpgradeCosts = upgradeCosts;
            RoomMinSize = roomMinSize;
            RoomMaxSize = roomMaxSize;
        }

        /// <summary>지금 강화 단계에서 다음 단계 비용. 최대면 −1.</summary>
        public int UpgradeCost(int currentUpgrade) => currentUpgrade < 0 || currentUpgrade >= UpgradeCosts.Count ? -1 : UpgradeCosts[currentUpgrade];
    }

    public enum WorkbenchResult
    {
        Done = 0,
        NotRelic = 1,
        NotEnoughEther = 2,
        BackpackFull = 3,
        MaxUpgrade = 4,
        NothingThere = 5,
    }

    /// <summary>제작대에서 한 일 (3-28b — HUD 로그).</summary>
    public enum WorkbenchAction
    {
        Craft = 0,
        Upgrade = 1,
        Disassemble = 2,
    }

    /// <summary>
    /// 마법 제작대 (3-28b, 결정 2026-11-05 — 사용자): 유물 만들기(목록에서, CraftCost 에테르) · 강화(+1 · +2 · +3 = UpgradeCosts) · 분해(배낭 유물 → DisassembleGain, 강화는 사라짐).
    /// 모래시계(Artifact)는 대상이 아니다. 모두 틱 밖 변경 — 입력이 부르고 이벤트를 비운다. 에테르는 RunState.Ether (스냅샷 v29), 여기로만 바꾼다.
    /// 작업실 = 구역마다 한 층 (보스 층 · 1층 · 제단 구역 제외) — 층은 판 시드로 계산 (난수 흐름을 쓰지 않아 다른 배치가 그대로).
    /// </summary>
    public static class Workbench
    {
        /// <summary>목록에서 골라 만든다 → 배낭에. 배낭이 가득이면 에테르를 쓰지 않는다.</summary>
        public static WorkbenchResult Craft(World world, ItemBook items, WorkbenchConfig config, Unit player, string relicId)
        {
            Validate(world, items, config, player);
            if (!Relics.IsRelic(items, relicId)) return WorkbenchResult.NotRelic;
            if (world.Run.Ether < config.CraftCost) return WorkbenchResult.NotEnoughEther;
            if (Inventory.TryAdd(world, null, items, player, InventoryRef.Item(relicId)) < 0) return WorkbenchResult.BackpackFull;
            ChangeEther(world, -config.CraftCost);
            world.Events.Publish(new WorkbenchUsedEvent(WorkbenchAction.Craft, relicId, 0, world.Run.Ether));
            return WorkbenchResult.Done;
        }

        /// <summary>배낭 inventoryIndex의 유물을 분해 → 에테르 + DisassembleGain.</summary>
        public static WorkbenchResult Disassemble(World world, ItemBook items, WorkbenchConfig config, Unit player, int inventoryIndex)
        {
            Validate(world, items, config, player);
            if (inventoryIndex < 0 || inventoryIndex >= player.InventorySlots.Length) return WorkbenchResult.NothingThere;
            var entry = player.InventorySlots[inventoryIndex];
            if (entry.Kind != InventoryKind.Item || !Relics.IsRelic(items, entry.Id)) return WorkbenchResult.NotRelic;
            Inventory.RemoveAt(world, player, inventoryIndex);
            ChangeEther(world, config.DisassembleGain);
            world.Events.Publish(new WorkbenchUsedEvent(WorkbenchAction.Disassemble, entry.Id, entry.Upgrade, world.Run.Ether));
            return WorkbenchResult.Done;
        }

        /// <summary>강화: 낀 유물(relicSlot ≥ 0) 또는 배낭 유물(inventoryIndex). 낀 생명석이면 최대 체력도 바로.</summary>
        public static WorkbenchResult Upgrade(World world, ItemBook items, WorkbenchConfig config, Unit player, int relicSlot, int inventoryIndex)
        {
            Validate(world, items, config, player);
            bool worn = relicSlot >= 0;
            InventoryRef entry;
            if (worn)
            {
                if (relicSlot >= player.Relics.Length) return WorkbenchResult.NothingThere;
                entry = player.Relics[relicSlot];
            }
            else
            {
                if (inventoryIndex < 0 || inventoryIndex >= player.InventorySlots.Length) return WorkbenchResult.NothingThere;
                entry = player.InventorySlots[inventoryIndex];
            }
            if (entry.IsEmpty || entry.Kind != InventoryKind.Item || !Relics.IsRelic(items, entry.Id)) return WorkbenchResult.NotRelic;
            int cost = config.UpgradeCost(entry.Upgrade);
            if (cost < 0) return WorkbenchResult.MaxUpgrade;
            if (world.Run.Ether < cost) return WorkbenchResult.NotEnoughEther;

            ChangeEther(world, -cost);
            var upgraded = InventoryRef.Item(entry.Id, entry.Upgrade + 1);
            if (worn) Relics.ReplaceWorn(items, player, relicSlot, upgraded);
            else
            {
                Inventory.RemoveAt(world, player, inventoryIndex);
                Inventory.TryAdd(world, null, items, player, upgraded); // 방금 빈 칸이 있으니 들어간다
            }
            world.Events.Publish(new WorkbenchUsedEvent(WorkbenchAction.Upgrade, entry.Id, upgraded.Upgrade, world.Run.Ether));
            if (worn) world.Events.Publish(new RelicEquippedEvent(player.Id, relicSlot, entry.Id, null, player.Hp, player.MaxHp, silent: true));
            return WorkbenchResult.Done;
        }

        private static void ChangeEther(World world, int delta)
        {
            world.Run.Ether = Math.Max(0, world.Run.Ether + delta);
            world.Events.Publish(new EtherChangedEvent(world.Run.Ether, delta));
        }

        private static void Validate(World world, ItemBook items, WorkbenchConfig config, Unit player)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (items == null) throw new ArgumentNullException(nameof(items));
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (player == null) throw new ArgumentNullException(nameof(player));
        }

        // ── 작업실 (구역마다 한 층) ──

        /// <summary>
        /// 그 층이 작업실 층인지: 층의 구역에서 1층 · 보스 층을 뺀 층 중 하나 — 판 시드와 구역 첫 층으로 정한다 (SplitMix64, 난수 흐름 안 씀).
        /// 제단 구역 · 구역 표가 없으면 false.
        /// </summary>
        public static bool IsWorkshopFloor(ZoneBook zones, ulong seed, int floor)
        {
            if (zones == null || floor < 2) return false;
            var zone = zones.ZoneOf(floor);
            if (zone.IsAltar) return false;
            var candidates = new List<int>();
            for (int f = Math.Max(2, zone.FirstFloor); f <= zone.LastFloor; f++)
                if (!zones.IsBossFloor(f)) candidates.Add(f);
            if (candidates.Count == 0) return false;
            ulong h = Mix(seed ^ (ulong)zone.FirstFloor * 0x9E3779B97F4A7C15UL);
            return candidates[(int)(h % (ulong)candidates.Count)] == floor;
        }

        private static ulong Mix(ulong z)
        {
            z += 0x9E3779B97F4A7C15UL;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }

        /// <summary>작업실 방을 판다 (꾸미기 안, Decor 난수 — 상점과 같은 파기): 가운데 칸 = 마법 제작대. 못 팠으면 false.</summary>
        public static bool BuildRoom(World world, DungeonFloor floor, WorkbenchConfig config, IRng rng)
        {
            if (world == null || floor == null || config == null || rng == null) return false;
            var room = SpecialRooms.CarveEmpty(world, floor, rng, config.RoomMinSize, config.RoomMaxSize, TileType.Door);
            if (room == null) return false;
            world.SetTile(room.Cells[room.Cells.Count / 2], TileType.ArtifactBench);
            return true;
        }
    }

    /// <summary>에테르가 바뀌었다 (3-28b — HUD). Ether = 바뀐 뒤, Delta = 변화 (0 = 이어하기 때 화면 맞춤).</summary>
    public sealed class EtherChangedEvent : ISimEvent
    {
        public int Ether { get; }
        public int Delta { get; }

        public EtherChangedEvent(int ether, int delta)
        {
            Ether = ether;
            Delta = delta;
        }
    }

    /// <summary>제작대를 썼다 (3-28b — HUD 로그). Upgrade = 결과 강화 단계 (분해면 분해한 유물의 단계).</summary>
    public sealed class WorkbenchUsedEvent : ISimEvent
    {
        public WorkbenchAction Action { get; }
        public string ItemId { get; }
        public int Upgrade { get; }
        public int Ether { get; }

        public WorkbenchUsedEvent(WorkbenchAction action, string itemId, int upgrade, int ether)
        {
            Action = action;
            ItemId = itemId;
            Upgrade = upgrade;
            Ether = ether;
        }
    }
}
