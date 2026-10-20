using System;
using System.IO;
using System.Text;
using ProjectM.Items;
using ProjectM.Units;

namespace ProjectM.Core
{
    /// <summary>
    /// WorldSnapshot ↔ 바이트. 같은 상태는 항상 같은 바이트가 된다 (리틀 엔디언, 고정 순서) — 결정론 검사에 쓴다.
    /// 세이브 파일 형식은 Phase 6에서 정한다. 이 형식은 그때 그대로 쓰거나 감쌀 수 있다.
    /// </summary>
    public static class WorldSnapshotCodec
    {
        private static readonly byte[] Magic = Encoding.ASCII.GetBytes("PMWS");

        public static byte[] Encode(WorldSnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));

            using (var stream = new MemoryStream())
            using (var w = new BinaryWriter(stream, Encoding.UTF8))
            {
                w.Write(Magic);
                w.Write(snapshot.FormatVersion);
                w.Write(snapshot.CurrentTick);

                w.Write(snapshot.MapWidth);
                w.Write(snapshot.MapHeight);
                w.Write(snapshot.Cells.Length);
                foreach (var cell in snapshot.Cells)
                    w.Write((int)cell);
                WriteExplored(w, snapshot.Explored);
                WriteTerrain(w, snapshot.Terrain);

                w.Write(snapshot.NextEntityId);

                w.Write(snapshot.RngSeed);
                w.Write(snapshot.RngStreams.Length);
                foreach (var s in snapshot.RngStreams)
                {
                    w.Write((int)s.Stream);
                    w.Write(s.State.State);
                    w.Write(s.State.Increment);
                }

                WriteActors(w, snapshot.Actors);
                WriteUnits(w, snapshot.Units);

                // 9 (3-4a): 층
                w.Write(snapshot.Floor);
                w.Write(snapshot.StoredFloors.Length);
                foreach (var f in snapshot.StoredFloors)
                {
                    w.Write(f.Floor);
                    w.Write(f.MapWidth);
                    w.Write(f.MapHeight);
                    w.Write(f.Cells.Length);
                    foreach (var cell in f.Cells)
                        w.Write((int)cell);
                    WriteExplored(w, f.Explored);
                    WriteTerrain(w, f.Terrain);
                    WriteActors(w, f.Actors);
                    WriteUnits(w, f.Units);
                    WriteFloorItems(w, f.FloorItems); // 11 (3-5)
                }

                // 10 (3-4b): 한 판 기록
                w.Write(snapshot.Kills);
                w.Write((int)snapshot.Outcome);
                w.Write(snapshot.BossId.Value);
                w.Write((int)snapshot.LastPlayerDamage.Kind);
                w.Write(snapshot.LastPlayerDamage.Id ?? string.Empty);
                w.Write((int)snapshot.LastPlayerDamage.Status);
                w.Write((int)snapshot.LastPlayerDamage.Terrain);
                w.Write(snapshot.LastPlayerAttacker.Value);

                // 11 (3-5): 바닥 아이템 · 마법서 배치 계획
                WriteFloorItems(w, snapshot.FloorItems);
                w.Write(snapshot.PendingFloorItems.Length);
                foreach (var p in snapshot.PendingFloorItems)
                {
                    w.Write(p.Floor);
                    w.Write(p.ItemId ?? string.Empty);
                }

                // 16 (3-9): 식별
                w.Write(snapshot.ItemLooks.Length);
                foreach (var look in snapshot.ItemLooks)
                {
                    w.Write(look.ItemId ?? string.Empty);
                    w.Write(look.Look ?? string.Empty);
                }
                w.Write(snapshot.IdentifiedItems.Length);
                foreach (var id in snapshot.IdentifiedItems)
                    w.Write(id ?? string.Empty);

                w.Flush();
                return stream.ToArray();
            }
        }

        public static WorldSnapshot Decode(byte[] bytes)
        {
            if (bytes == null) throw new ArgumentNullException(nameof(bytes));

            using (var stream = new MemoryStream(bytes))
            using (var r = new BinaryReader(stream, Encoding.UTF8))
            {
                var magic = r.ReadBytes(Magic.Length);
                for (int i = 0; i < Magic.Length; i++)
                {
                    if (magic.Length != Magic.Length || magic[i] != Magic[i])
                        throw new InvalidDataException("WorldSnapshot 데이터가 아니다.");
                }

                var s = new WorldSnapshot { FormatVersion = r.ReadInt32() };
                if (s.FormatVersion != WorldSnapshot.CurrentFormatVersion)
                    throw new InvalidDataException($"지원하지 않는 스냅샷 버전 {s.FormatVersion} (현재 {WorldSnapshot.CurrentFormatVersion}).");

                s.CurrentTick = r.ReadInt64();

                s.MapWidth = r.ReadInt32();
                s.MapHeight = r.ReadInt32();
                s.Cells = ReadCells(r);
                s.Explored = ReadExplored(r);
                s.Terrain = ReadTerrain(r);

                s.NextEntityId = r.ReadInt32();

                s.RngSeed = r.ReadUInt64();
                s.RngStreams = new RngStreamState[ReadCount(r)];
                for (int i = 0; i < s.RngStreams.Length; i++)
                {
                    var rngStream = (RngStream)r.ReadInt32();
                    var state = r.ReadUInt64();
                    var increment = r.ReadUInt64();
                    s.RngStreams[i] = new RngStreamState(rngStream, new Pcg32State(state, increment));
                }

                s.Actors = ReadActors(r);
                s.Units = ReadUnits(r);

                s.Floor = r.ReadInt32();
                s.StoredFloors = new StoredFloorState[ReadCount(r)];
                for (int i = 0; i < s.StoredFloors.Length; i++)
                {
                    var f = new StoredFloorState { Floor = r.ReadInt32(), MapWidth = r.ReadInt32(), MapHeight = r.ReadInt32() };
                    f.Cells = ReadCells(r);
                    f.Explored = ReadExplored(r);
                    f.Terrain = ReadTerrain(r);
                    f.Actors = ReadActors(r);
                    f.Units = ReadUnits(r);
                    f.FloorItems = ReadFloorItems(r);
                    s.StoredFloors[i] = f;
                }

                s.Kills = r.ReadInt32();
                s.Outcome = (RunOutcome)r.ReadInt32();
                s.BossId = new EntityId(r.ReadInt32());
                var kind = (DamageKind)r.ReadInt32();
                var causeId = r.ReadString();
                var causeStatus = (ProjectM.Status.StatusType)r.ReadInt32();
                var causeTerrain = (ProjectM.Status.TerrainEffectType)r.ReadInt32();
                s.LastPlayerDamage = new DamageCause(kind, causeId, causeStatus, causeTerrain);
                s.LastPlayerAttacker = new EntityId(r.ReadInt32());

                s.FloorItems = ReadFloorItems(r);
                s.PendingFloorItems = new PendingFloorItem[ReadCount(r)];
                for (int i = 0; i < s.PendingFloorItems.Length; i++)
                {
                    int floor = r.ReadInt32();
                    s.PendingFloorItems[i] = new PendingFloorItem(floor, r.ReadString());
                }

                s.ItemLooks = new ItemLook[ReadCount(r)];
                for (int i = 0; i < s.ItemLooks.Length; i++)
                {
                    var itemId = r.ReadString();
                    s.ItemLooks[i] = new ItemLook(itemId, r.ReadString());
                }
                s.IdentifiedItems = new string[ReadCount(r)];
                for (int i = 0; i < s.IdentifiedItems.Length; i++)
                    s.IdentifiedItems[i] = r.ReadString();

                if (stream.Position != stream.Length)
                    throw new InvalidDataException("스냅샷 끝에 남는 데이터가 있다.");
                return s;
            }
        }

        // ── 공용 조각 (지금 층 · 떠난 층이 같이 씀, 3-4a에서 나눔 — 바이트 순서는 v8과 같음) ──

        private static TileType[] ReadCells(BinaryReader r)
        {
            var cells = new TileType[ReadCount(r)];
            for (int i = 0; i < cells.Length; i++)
                cells[i] = (TileType)r.ReadInt32();
            return cells;
        }

        private static void WriteExplored(BinaryWriter w, bool[] explored)
        {
            w.Write(explored.Length);
            foreach (var seen in explored)
                w.Write(seen);
        }

        private static bool[] ReadExplored(BinaryReader r)
        {
            var explored = new bool[ReadCount(r)];
            for (int i = 0; i < explored.Length; i++)
                explored[i] = r.ReadBoolean();
            return explored;
        }

        private static void WriteTerrain(BinaryWriter w, TerrainCellState[] terrain)
        {
            w.Write(terrain.Length);
            foreach (var t in terrain)
            {
                w.Write(t.CellIndex);
                w.Write((int)t.Type);
                w.Write(t.Remaining);
            }
        }

        private static TerrainCellState[] ReadTerrain(BinaryReader r)
        {
            var terrain = new TerrainCellState[ReadCount(r)];
            for (int i = 0; i < terrain.Length; i++)
            {
                int cell = r.ReadInt32();
                var type = (ProjectM.Status.TerrainEffectType)r.ReadInt32();
                terrain[i] = new TerrainCellState(cell, type, r.ReadInt32());
            }
            return terrain;
        }

        private static void WriteActors(BinaryWriter w, ActorState[] actors)
        {
            w.Write(actors.Length);
            foreach (var a in actors)
            {
                w.Write(a.Id.Value);
                w.Write(a.ReadyTick);
                w.Write(a.IsPlayer);
            }
        }

        private static ActorState[] ReadActors(BinaryReader r)
        {
            var actors = new ActorState[ReadCount(r)];
            for (int i = 0; i < actors.Length; i++)
            {
                var id = new EntityId(r.ReadInt32());
                var readyTick = r.ReadInt64();
                var isPlayer = r.ReadBoolean();
                actors[i] = new ActorState(id, readyTick, isPlayer);
            }
            return actors;
        }

        private static void WriteUnits(BinaryWriter w, UnitState[] units)
        {
            w.Write(units.Length);
            foreach (var u in units)
            {
                w.Write(u.Id.Value);
                w.Write((int)u.Faction);
                w.Write(u.VisualId);
                w.Write(u.MaxHp);
                w.Write(u.AttackDamage);
                w.Write(u.SightRange);
                w.Write(u.X);
                w.Write(u.Y);
                w.Write(u.Hp);
                w.Write(u.HasLastSeen);
                w.Write(u.LastSeenX);
                w.Write(u.LastSeenY);
                w.Write(u.MaxMana);
                w.Write(u.Mana);
                WriteRefs(w, u.InventorySlots);
                WriteRefs(w, u.QuickSlots);
                WriteOptionalPos(w, u.PreviousPos);
                WriteOptionalPos(w, u.WanderTarget);
                w.Write(u.Statuses.Length);
                foreach (var st in u.Statuses)
                {
                    w.Write((int)st.Type);
                    w.Write(st.Remaining);
                }
                // 12 (3-6 · 3-7)
                w.Write(u.Accuracy);
                w.Write(u.Evasion);
                w.Write(u.ExpReward);
                w.Write(u.ExpMaxLevel);
                w.Write(u.Level);
                w.Write(u.Exp);
                // 13 (3-7a): 든 무기
                w.Write(!u.Weapon.IsEmpty);
                if (!u.Weapon.IsEmpty)
                {
                    w.Write(u.Weapon.ItemId);
                    w.Write(u.Weapon.MinDamage);
                    w.Write(u.Weapon.MaxDamage);
                    w.Write(u.Weapon.AccuracyPercent);
                    w.Write(u.Weapon.AttackCostTicks);
                    w.Write(u.Weapon.Reach);
                    w.Write(u.Weapon.Upgrade); // 14 (3-7b)
                    w.Write(u.Weapon.StrengthRequirement);
                    w.Write(u.Weapon.SurpriseMinPercent); // 15 (3-7c)
                    w.Write(u.Weapon.Defense);
                }
                w.Write(u.Strength); // 14 (3-7b)
                // 17 (3-10a): 입은 갑옷
                w.Write(!u.Armor.IsEmpty);
                if (!u.Armor.IsEmpty)
                {
                    w.Write(u.Armor.ItemId);
                    w.Write(u.Armor.Tier);
                    w.Write(u.Armor.Upgrade);
                    w.Write(u.Armor.StrengthRequirement);
                    w.Write(u.Armor.DefenseMin);
                    w.Write(u.Armor.DefenseMax);
                    w.Write(u.Armor.DeficitPenalty);
                    w.Write(u.Armor.DefenseDivisor);
                }
            }
        }

        private static UnitState[] ReadUnits(BinaryReader r)
        {
            var units = new UnitState[ReadCount(r)];
            for (int i = 0; i < units.Length; i++)
            {
                var id = new EntityId(r.ReadInt32());
                var faction = (Faction)r.ReadInt32();
                var visualId = r.ReadString();
                var maxHp = r.ReadInt32();
                var attackDamage = r.ReadInt32();
                var sightRange = r.ReadInt32();
                var x = r.ReadInt32();
                var y = r.ReadInt32();
                var hp = r.ReadInt32();
                var hasLastSeen = r.ReadBoolean();
                var lastSeenX = r.ReadInt32();
                var lastSeenY = r.ReadInt32();
                var maxMana = r.ReadInt32();
                var mana = r.ReadInt32();
                var inventory = ReadRefs(r);
                var quick = ReadRefs(r);
                var previousPos = ReadOptionalPos(r);
                var wanderTarget = ReadOptionalPos(r);
                var statuses = new UnitStatusState[ReadCount(r)];
                for (int k = 0; k < statuses.Length; k++)
                {
                    var type = (ProjectM.Status.StatusType)r.ReadInt32();
                    statuses[k] = new UnitStatusState(type, r.ReadInt32());
                }
                int accuracy = r.ReadInt32(), evasion = r.ReadInt32(), expReward = r.ReadInt32(), expMaxLevel = r.ReadInt32();
                int level = r.ReadInt32(), exp = r.ReadInt32();
                var weapon = EquippedWeapon.Empty;
                if (r.ReadBoolean())
                {
                    var weaponId = r.ReadString();
                    int wMin = r.ReadInt32(), wMax = r.ReadInt32(), wAcc = r.ReadInt32(), wCost = r.ReadInt32(), wReach = r.ReadInt32();
                    int wUpgrade = r.ReadInt32(), wStr = r.ReadInt32(), wSurprise = r.ReadInt32(), wDefense = r.ReadInt32();
                    weapon = new EquippedWeapon(weaponId, wMin, wMax, wAcc, wCost, wReach, wUpgrade, wStr, wSurprise, wDefense);
                }
                int strength = r.ReadInt32();
                var armor = EquippedArmor.Empty;
                if (r.ReadBoolean())
                {
                    var armorId = r.ReadString();
                    int aTier = r.ReadInt32(), aUpgrade = r.ReadInt32(), aStr = r.ReadInt32(), aMin = r.ReadInt32(), aMax = r.ReadInt32();
                    int aPenalty = r.ReadInt32(), aDivisor = r.ReadInt32();
                    armor = new EquippedArmor(armorId, aTier, aUpgrade, aStr, aMin, aMax, aPenalty, aDivisor);
                }
                units[i] = new UnitState(id, faction, visualId, maxHp, attackDamage, sightRange, x, y, hp, hasLastSeen, lastSeenX, lastSeenY,
                    maxMana, mana, inventory, quick, previousPos, wanderTarget, statuses, accuracy, evasion, expReward, expMaxLevel, level, exp, weapon, strength, armor);
            }
            return units;
        }

        // 바닥 아이템 (3-5): 개수 + x · y · 아이템 ID
        private static void WriteFloorItems(BinaryWriter w, FloorItemState[] items)
        {
            w.Write(items.Length);
            foreach (var item in items)
            {
                w.Write(item.X);
                w.Write(item.Y);
                w.Write(item.ItemId ?? string.Empty);
            }
        }

        private static FloorItemState[] ReadFloorItems(BinaryReader r)
        {
            var items = new FloorItemState[ReadCount(r)];
            for (int i = 0; i < items.Length; i++)
            {
                int x = r.ReadInt32();
                int y = r.ReadInt32();
                items[i] = new FloorItemState(x, y, r.ReadString());
            }
            return items;
        }

        // 인벤토리 · 빠른 칸 (3-2a): 개수 + 칸마다 종류(int) · ID(빈 칸 = 빈 문자열)
        private static void WriteRefs(BinaryWriter w, InventoryRef[] refs)
        {
            w.Write(refs.Length);
            foreach (var entry in refs)
            {
                w.Write((int)entry.Kind);
                w.Write(entry.Id ?? string.Empty);
                w.Write(entry.Upgrade); // 14 (3-7b)
            }
        }

        private static InventoryRef[] ReadRefs(BinaryReader r)
        {
            var refs = new InventoryRef[ReadCount(r)];
            for (int k = 0; k < refs.Length; k++)
            {
                var kind = (InventoryKind)r.ReadInt32();
                var id = r.ReadString();
                if (!Enum.IsDefined(typeof(InventoryKind), kind)) throw new InvalidDataException("알 수 없는 인벤토리 종류 " + (int)kind);
                int upgrade = r.ReadInt32(); // 14 (3-7b)
                refs[k] = kind == InventoryKind.None ? InventoryRef.Empty : new InventoryRef(kind, id, upgrade);
            }
            return refs;
        }

        // 있음 여부(bool) + x · y. 없으면 0, 0을 써서 바이트 길이를 고정한다.
        private static void WriteOptionalPos(BinaryWriter w, GridPos? pos)
        {
            w.Write(pos.HasValue);
            w.Write(pos?.X ?? 0);
            w.Write(pos?.Y ?? 0);
        }

        private static GridPos? ReadOptionalPos(BinaryReader r)
        {
            bool has = r.ReadBoolean();
            int x = r.ReadInt32();
            int y = r.ReadInt32();
            return has ? new GridPos(x, y) : (GridPos?)null;
        }

        private static int ReadCount(BinaryReader r)
        {
            int count = r.ReadInt32();
            if (count < 0 || count > r.BaseStream.Length) throw new InvalidDataException("잘못된 개수: " + count);
            return count;
        }
    }
}
