using System;
using System.Collections;
using System.Threading;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Cysharp.Threading.Tasks;
using Laboratory.Core.Services;
using Laboratory.Core.Events;
using Laboratory.Core.Events.Messages;

#nullable enable

namespace Laboratory.Core.Tests.Unit.Services
{
    /// <summary>
    /// Unit tests for the AssetService implementation.
    /// </summary>
    public class AssetServiceTests
    {
        private MockEventBus? _mockEventBus;
        private AssetService? _assetService;

        [SetUp]
        public void SetUp()
        {
            _mockEventBus = new MockEventBus();
            _assetService = new AssetService(_mockEventBus);
        }

        [TearDown]
        public void TearDown()
        {
            _assetService?.Dispose();
            _mockEventBus?.Dispose();
        }

        #region Constructor Tests

        [Test]
        public void Constructor_NullEventBus_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new AssetService(null!));
        }

        [Test]
        public void Constructor_ValidEventBus_CreatesInstance()
        {
            // Act
            using var service = new AssetService(_mockEventBus!);

            // Assert
            Assert.IsNotNull(service);
        }

        #endregion

        #region Cache Tests

        [Test]
        public void IsAssetCached_NotCachedAsset_ReturnsFalse()
        {
            // Act
            var result = _assetService!.IsAssetCached("nonexistent");

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void GetCachedAsset_NotCachedAsset_ReturnsNull()
        {
            // Act
            var result = _assetService!.GetCachedAsset<Texture2D>("nonexistent");

            // Assert
            Assert.IsNull(result);
        }

        [Test]
        public void UnloadAsset_NotCachedAsset_DoesNotThrow()
        {
            // Act & Assert (should not throw)
            _assetService!.UnloadAsset("nonexistent");
        }

        [Test]
        public void ClearCache_EmptyCache_DoesNotThrow()
        {
            // Act & Assert (should not throw)
            _assetService!.ClearCache();
        }

        [Test]
        public void GetCacheStats_EmptyCache_ReturnsZeroStats()
        {
            // Act
            var stats = _assetService!.GetCacheStats();

            // Assert
            Assert.AreEqual(0, stats.TotalAssets);
            Assert.AreEqual(0, stats.TotalMemoryUsage);
            Assert.AreEqual(0, stats.ResourcesAssets);
            Assert.AreEqual(0, stats.AddressableAssets);
        }

        #endregion

        #region Loading Tests

        [UnityTest]
        public IEnumerator LoadAssetAsync_InvalidKey_ReturnsNull()
        {
            // Act
            var task = _assetService!.LoadAssetAsync<Texture2D>("invalid/path", AssetSource.Resources);
            yield return task.ToCoroutine();

            // Assert
            Assert.IsNull(task.GetAwaiter().GetResult());
        }

        [UnityTest]
        public IEnumerator PreloadCoreAssetsAsync_ValidService_PublishesEvents()
        {
            // Arrange
            var progress = new TestProgress();
            var cancellation = CancellationToken.None;

            // Act
            var task = _assetService!.PreloadCoreAssetsAsync(progress, cancellation);
            yield return task.ToCoroutine();

            // Assert
            Assert.IsTrue(progress.WasReported);
            Assert.IsTrue(_mockEventBus!.WasEventPublished<LoadingCompletedEvent>());
        }

        #endregion

        #region Disposal Tests

        [Test]
        public void Dispose_ValidService_DisposesSuccessfully()
        {
            // Act & Assert (should not throw)
            _assetService!.Dispose();
        }

        [Test]
        public void Operations_AfterDisposal_ThrowObjectDisposedException()
        {
            // Arrange
            _assetService!.Dispose();

            // Act & Assert
            Assert.Throws<ObjectDisposedException>(() => 
                _assetService.IsAssetCached("test"));
        }

        #endregion
    }

    /// <summary>
    /// Mock implementation of IEventBus for testing.
    /// </summary>
    public class MockEventBus : IEventBus
    {
        private readonly System.Collections.Generic.HashSet<Type> _publishedEvents = new();
        private bool _disposed = false;

        public void Publish<T>(T message) where T : class
        {
            _publishedEvents.Add(typeof(T));
        }

        public bool WasEventPublished<T>() where T : class
        {
            return _publishedEvents.Contains(typeof(T));
        }

        public IDisposable Subscribe<T>(Action<T> handler) where T : class
        {
            return new MockDisposable();
        }

        public UniRx.IObservable<T> Observe<T>() where T : class
        {
            return UniRx.Observable.Empty<T>();
        }

        public IDisposable SubscribeOnMainThread<T>(Action<T> handler) where T : class
        {
            return new MockDisposable();
        }

        public IDisposable SubscribeWhere<T>(Func<T, bool> predicate, Action<T> handler) where T : class
        {
            return new MockDisposable();
        }

        public IDisposable SubscribeFirst<T>(Action<T> handler) where T : class
        {
            return new MockDisposable();
        }

        public int GetSubscriberCount<T>() where T : class
        {
            return 0;
        }

        public void ClearSubscriptions<T>() where T : class
        {
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _publishedEvents.Clear();
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Mock disposable for testing.
    /// </summary>
    public class MockDisposable : IDisposable
    {
        public void Dispose() { }
    }

    /// <summary>
    /// Test progress reporter.
    /// </summary>
    public class TestProgress : IProgress<float>
    {
        public bool WasReported { get; private set; }
        public float LastValue { get; private set; }

        public void Report(float value)
        {
            WasReported = true;
            LastValue = value;
        }
    }
}
