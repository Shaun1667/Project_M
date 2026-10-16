using System;
using System.IO;
using System.Text;
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
                w.Write(snapshot.Explored.Length);
                foreach (var seen in snapshot.Explored)
                    w.Write(seen);

                w.Write(snapshot.NextEntityId);

                w.Write(snapshot.RngSeed);
                w.Write(snapshot.RngStreams.Length);
                foreach (var s in snapshot.RngStreams)
                {
                    w.Write((int)s.Stream);
                    w.Write(s.State.State);
                    w.Write(s.State.Increment);
                }

                w.Write(snapshot.Actors.Length);
                foreach (var a in snapshot.Actors)
                {
                    w.Write(a.Id.Value);
                    w.Write(a.ReadyTick);
                    w.Write(a.IsPlayer);
                }

                w.Write(snapshot.Units.Length);
                foreach (var u in snapshot.Units)
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
                }

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
                s.Cells = new TileType[ReadCount(r)];
                for (int i = 0; i < s.Cells.Length; i++)
                    s.Cells[i] = (TileType)r.ReadInt32();
                s.Explored = new bool[ReadCount(r)];
                for (int i = 0; i < s.Explored.Length; i++)
                    s.Explored[i] = r.ReadBoolean();

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

                s.Actors = new ActorState[ReadCount(r)];
                for (int i = 0; i < s.Actors.Length; i++)
                {
                    var id = new EntityId(r.ReadInt32());
                    var readyTick = r.ReadInt64();
                    var isPlayer = r.ReadBoolean();
                    s.Actors[i] = new ActorState(id, readyTick, isPlayer);
                }

                s.Units = new UnitState[ReadCount(r)];
                for (int i = 0; i < s.Units.Length; i++)
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
                    s.Units[i] = new UnitState(id, faction, visualId, maxHp, attackDamage, sightRange, x, y, hp, hasLastSeen, lastSeenX, lastSeenY);
                }

                if (stream.Position != stream.Length)
                    throw new InvalidDataException("스냅샷 끝에 남는 데이터가 있다.");
                return s;
            }
        }

        private static int ReadCount(BinaryReader r)
        {
            int count = r.ReadInt32();
            if (count < 0 || count > r.BaseStream.Length) throw new InvalidDataException("잘못된 개수: " + count);
            return count;
        }
    }
}
