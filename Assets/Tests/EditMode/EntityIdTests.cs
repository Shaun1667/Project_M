using System;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectM.Core;

namespace ProjectM.Tests
{
    public class EntityIdTests
    {
        [Test]
        public void None_IsInvalid()
        {
            Assert.That(EntityId.None.IsValid, Is.False);
            Assert.That(default(EntityId), Is.EqualTo(EntityId.None));
        }

        [Test]
        public void Allocator_IssuesUniqueIdsFromOne()
        {
            var allocator = new EntityIdAllocator();
            var seen = new HashSet<EntityId>();
            for (int i = 0; i < 1000; i++)
            {
                var id = allocator.Allocate();
                Assert.That(id.IsValid, Is.True);
                Assert.That(seen.Add(id), Is.True, "중복 " + id);
            }
            Assert.That(seen.Contains(new EntityId(1)), Is.True);
            Assert.That(allocator.NextValue, Is.EqualTo(1001));
        }

        [Test]
        public void Allocator_RestoredFromNextValue_ContinuesWithoutReuse()
        {
            var original = new EntityIdAllocator();
            original.Allocate();
            original.Allocate();

            var restored = new EntityIdAllocator(original.NextValue);
            Assert.That(restored.Allocate(), Is.EqualTo(original.Allocate()));
        }

        [Test]
        public void Allocator_RejectsInvalidStart()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new EntityIdAllocator(0));
        }
    }
}
