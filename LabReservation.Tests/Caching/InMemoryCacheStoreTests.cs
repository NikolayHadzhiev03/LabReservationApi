using LabReservation.BL.Services.KafkaCache;
using LabReservation.Models.Entities;
using Xunit;

namespace LabReservation.Tests.Caching
{
    public class InMemoryCacheStoreTests
    {
        private static InMemoryCacheStore<Lab> CreateStore() =>
            new(l => l.Id);

        private static Lab Lab(string id, string name = "Lab", bool available = true) =>
            new() { Id = id, Name = name, IsAvailable = available };

        [Fact]
        public void ReplaceAll_PopulatesStore_AndIsLookupable()
        {
            var store = CreateStore();

            store.ReplaceAll(new[] { Lab("1", "Physics"), Lab("2", "Chemistry") });

            Assert.Equal(2, store.Count);
            Assert.True(store.TryGet("1", out var found));
            Assert.Equal("Physics", found!.Name);
            Assert.Equal(2, store.GetAll().Count);
        }

        [Fact]
        public void ReplaceAll_ReplacesPreviousContents()
        {
            var store = CreateStore();
            store.ReplaceAll(new[] { Lab("1"), Lab("2") });

            store.ReplaceAll(new[] { Lab("3") });

            Assert.Equal(1, store.Count);
            Assert.False(store.TryGet("1", out _));
            Assert.True(store.TryGet("3", out _));
        }

        [Fact]
        public void Upsert_AddsNew_AndUpdatesExisting()
        {
            var store = CreateStore();
            store.ReplaceAll(new[] { Lab("1", "Physics") });

            store.Upsert(Lab("2", "Biology"));
            store.Upsert(Lab("1", "Physics II"));

            Assert.Equal(2, store.Count);
            Assert.True(store.TryGet("1", out var updated));
            Assert.Equal("Physics II", updated!.Name);
            Assert.True(store.TryGet("2", out _));
        }

        [Fact]
        public void TryGet_MissingOrNullKey_ReturnsFalse()
        {
            var store = CreateStore();
            store.ReplaceAll(new[] { Lab("1") });

            Assert.False(store.TryGet("does-not-exist", out var missing));
            Assert.Null(missing);
            Assert.False(store.TryGet(null!, out _));
        }

        [Fact]
        public void EmptyStore_HasZeroCount()
        {
            var store = CreateStore();
            Assert.Equal(0, store.Count);
            Assert.Empty(store.GetAll());
        }
    }
}
