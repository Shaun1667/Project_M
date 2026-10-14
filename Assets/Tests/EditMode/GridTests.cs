using System;
using NUnit.Framework;
using ProjectM.Core;

namespace ProjectM.Tests
{
    public class GridTests
    {
        [Test]
        public void GridPos_EqualityAndArithmetic()
        {
            var a = new GridPos(3, 4);
            Assert.That(a, Is.EqualTo(new GridPos(3, 4)));
            Assert.That(a == new GridPos(3, 4), Is.True);
            Assert.That(a != new GridPos(4, 3), Is.True);
            Assert.That(a + new GridPos(1, -1), Is.EqualTo(new GridPos(4, 3)));
            Assert.That(a - new GridPos(3, 4), Is.EqualTo(new GridPos(0, 0)));
        }

        [Test]
        public void GridMap_RejectsNonPositiveSize()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new GridMap(0, 5));
            Assert.Throws<ArgumentOutOfRangeException>(() => new GridMap(5, -1));
        }

        [Test]
        public void GridMap_StartsEmpty_AndStoresTiles()
        {
            var map = new GridMap(4, 3);
            Assert.That(map.Get(new GridPos(3, 2)), Is.EqualTo(TileType.Empty));

            map.Set(new GridPos(3, 2), TileType.Wall);
            map.Set(new GridPos(0, 0), TileType.Floor);

            Assert.That(map.Get(new GridPos(3, 2)), Is.EqualTo(TileType.Wall));
            Assert.That(map.Get(new GridPos(0, 0)), Is.EqualTo(TileType.Floor));
            Assert.That(map.Get(new GridPos(2, 2)), Is.EqualTo(TileType.Empty), "다른 칸에 영향 없음 (x, y 인덱스 혼동 검사)");
        }

        [Test]
        public void GridMap_BoundsAndWalkability()
        {
            var map = new GridMap(2, 2);
            map.Set(new GridPos(0, 0), TileType.Floor);
            map.Set(new GridPos(1, 0), TileType.Wall);

            Assert.That(map.InBounds(new GridPos(1, 1)), Is.True);
            Assert.That(map.InBounds(new GridPos(2, 0)), Is.False);
            Assert.That(map.InBounds(new GridPos(0, -1)), Is.False);

            Assert.That(map.IsWalkable(new GridPos(0, 0)), Is.True);
            Assert.That(map.IsWalkable(new GridPos(1, 0)), Is.False, "벽");
            Assert.That(map.IsWalkable(new GridPos(0, 1)), Is.False, "빈칸");
            Assert.That(map.IsWalkable(new GridPos(-1, 0)), Is.False, "맵 밖");

            Assert.Throws<ArgumentOutOfRangeException>(() => map.Get(new GridPos(2, 0)));
            Assert.Throws<ArgumentOutOfRangeException>(() => map.Set(new GridPos(0, 2), TileType.Floor));
        }
    }
}
