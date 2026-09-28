using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;

namespace Presentation.Test.TestDoubles;

internal sealed class FakeSession : ISession
{
    private readonly Dictionary<string, byte[]> _values = [];

    public int CommitCount { get; private set; }

    public bool IsAvailable => true;
    public string Id { get; } = Guid.NewGuid().ToString();
    public IEnumerable<string> Keys => _values.Keys;

    public void Clear() => _values.Clear();
    public void Remove(string key) => _values.Remove(key);
    public void Set(string key, byte[] value) => _values[key] = value;
    public bool TryGetValue(string key, [NotNullWhen(true)] out byte[]? value) =>
        _values.TryGetValue(key, out value);

    public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
        CommitCount++;
        return Task.CompletedTask;
    }
}
