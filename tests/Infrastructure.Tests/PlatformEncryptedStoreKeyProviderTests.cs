using HasbeMaal.Infrastructure.Persistence;

namespace HasbeMaal.Infrastructure.Tests;

[TestClass]
public sealed class PlatformEncryptedStoreKeyProviderTests
{
    [TestMethod]
    public async Task GetOrCreateKeyAsync_WhenMissing_GeneratesAndPersistsBase64Key()
    {
        var protectedStore = new InMemoryProtectedKeyValueStore();
        var provider = new PlatformEncryptedStoreKeyProvider(protectedStore);

        var key = await provider.GetOrCreateKeyAsync();

        var persistedKey = await protectedStore.GetAsync(PlatformEncryptedStoreKeyProvider.DefaultKeyName);
        Assert.IsNotNull(persistedKey);
        Assert.HasCount(32, key);
        Assert.AreEqual(Convert.ToBase64String(key), persistedKey);
        Assert.AreEqual(1, protectedStore.SetCount);
    }

    [TestMethod]
    public async Task GetOrCreateKeyAsync_WhenExistingKeyIsValid_ReturnsPersistedKey()
    {
        var existingKey = Enumerable.Range(0, 32).Select(value => (byte)value).ToArray();
        var protectedStore = new InMemoryProtectedKeyValueStore();
        protectedStore.Seed(
            PlatformEncryptedStoreKeyProvider.DefaultKeyName,
            Convert.ToBase64String(existingKey));
        var provider = new PlatformEncryptedStoreKeyProvider(protectedStore);

        var key = await provider.GetOrCreateKeyAsync();

        Assert.AreEqual(Convert.ToBase64String(existingKey), Convert.ToBase64String(key));
        Assert.AreEqual(0, protectedStore.SetCount);
    }

    [TestMethod]
    public async Task GetOrCreateKeyAsync_WhenPersistedKeyIsMalformedBase64_ThrowsWithoutReplacing()
    {
        const string malformedKey = "not valid base64";
        var protectedStore = new InMemoryProtectedKeyValueStore();
        protectedStore.Seed(PlatformEncryptedStoreKeyProvider.DefaultKeyName, malformedKey);
        var provider = new PlatformEncryptedStoreKeyProvider(protectedStore);

        await Assert.ThrowsExactlyAsync<InvalidDataException>(async () =>
            await provider.GetOrCreateKeyAsync());

        Assert.AreEqual(
            malformedKey,
            await protectedStore.GetAsync(PlatformEncryptedStoreKeyProvider.DefaultKeyName));
        Assert.AreEqual(0, protectedStore.SetCount);
    }

    [TestMethod]
    public async Task GetOrCreateKeyAsync_WhenPersistedKeyHasWrongLength_ThrowsWithoutReplacing()
    {
        var wrongLengthKey = Convert.ToBase64String(new byte[16]);
        var protectedStore = new InMemoryProtectedKeyValueStore();
        protectedStore.Seed(PlatformEncryptedStoreKeyProvider.DefaultKeyName, wrongLengthKey);
        var provider = new PlatformEncryptedStoreKeyProvider(protectedStore);

        await Assert.ThrowsExactlyAsync<InvalidDataException>(async () =>
            await provider.GetOrCreateKeyAsync());

        Assert.AreEqual(
            wrongLengthKey,
            await protectedStore.GetAsync(PlatformEncryptedStoreKeyProvider.DefaultKeyName));
        Assert.AreEqual(0, protectedStore.SetCount);
    }

    [TestMethod]
    public async Task GetOrCreateKeyAsync_WhenFirstUseIsConcurrent_PersistsOneSharedKey()
    {
        var protectedStore = new InMemoryProtectedKeyValueStore();
        var provider = new PlatformEncryptedStoreKeyProvider(protectedStore);

        var keys = await Task.WhenAll(
            Enumerable.Range(0, 16).Select(_ => provider.GetOrCreateKeyAsync()));

        var expectedKey = Convert.ToBase64String(keys[0]);
        foreach (var key in keys)
        {
            Assert.AreEqual(expectedKey, Convert.ToBase64String(key));
        }

        Assert.AreEqual(1, protectedStore.SetCount);
        Assert.AreEqual(
            expectedKey,
            await protectedStore.GetAsync(PlatformEncryptedStoreKeyProvider.DefaultKeyName));
    }

    private sealed class InMemoryProtectedKeyValueStore : IProtectedKeyValueStore
    {
        private readonly object gate = new();
        private readonly Dictionary<string, string> values = new(StringComparer.Ordinal);
        private int setCount;

        public int SetCount
        {
            get
            {
                lock (gate)
                {
                    return setCount;
                }
            }
        }

        public Task<string?> GetAsync(string key, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            lock (gate)
            {
                values.TryGetValue(key, out var value);
                return Task.FromResult(value);
            }
        }

        public Task SetAsync(string key, string value, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            lock (gate)
            {
                values[key] = value;
                setCount++;
            }

            return Task.CompletedTask;
        }

        public void Seed(string key, string value)
        {
            lock (gate)
            {
                values[key] = value;
            }
        }
    }
}