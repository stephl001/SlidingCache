using FluentAssertions;
using SlidingTemporaryCache;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace SlidingCacheTests
{
    public sealed class CacheTests
    {
        [Fact]
        public void BasicCachingTests()
        {
            var sc = new SlidingCache(TimeSpan.FromMilliseconds(50));
            sc.Should().NotBeNull();

            sc.AddOrGetExisting("key1", () => "key1").Should().Be("key1");
            sc.AddOrGetExisting("key1", () => "key2").Should().Be("key1");
            sc.AddOrGetExisting("key3", () => "key3").Should().Be("key3");
            Thread.Sleep(51);
            sc.AddOrGetExisting("key1", () => "key2").Should().Be("key2");
        }

        [Fact]
        public void BasicSlidingCachingTests()
        {
            var sc = new SlidingCache(TimeSpan.FromSeconds(2));
            sc.Should().NotBeNull();

            sc.AddOrGetExisting("key1", () => "key1");
            for (int i = 0; i < 3; i++)
            {
                sc.AddOrGetExisting("key1", () => "key2").Should().Be("key1");
                Thread.Sleep(1000);
            }

            Thread.Sleep(1050);
            sc.AddOrGetExisting("key1", () => "key2").Should().Be("key2");
        }

        [Fact]
        public async Task MultithreadedCachingTests()
        {
            var sc = new SlidingCache(TimeSpan.FromMinutes(1));

            //Although 10 threads will ask the same key all at once with different value factory,
            //they should all get the exact same value.
            Guid[] values = await Task.WhenAll(Enumerable.Range(1, 10).Select(i => Task.Run(() => GetCachedValue(sc)))).ConfigureAwait(false);
            values.Distinct().Should().HaveCount(1);
        }

        private Task<Guid> GetCachedValue(SlidingCache sc)
        {
            return Task.FromResult(sc.AddOrGetExisting("single_key", () => Guid.NewGuid()));
        }

        [Fact]
        public void FactoryExceptionTests()
        {
            var sc = new SlidingCache(TimeSpan.FromHours(1));

            Action act = () => sc.AddOrGetExisting<string>("test", () => { throw new InvalidOperationException("Boom!"); });
            act.ShouldThrow<LazyInitializationException>();
            sc.AddOrGetExisting("test", () => "New Value").Should().Be("New Value");
        }
    }
}
