using System.IO;

using FluentAssertions;

using Stalinon.Bot.Storage.File.Options;

using Xunit;

using FileStateStore = Stalinon.Bot.Storage.File.FileStateStore;

namespace Stalinon.Bot.Storage.File.Tests;

/// <summary>
///	Тесты CRUD операций файлового хранилища
/// </summary>
/// <remarks>
///	<list type="number">
///	<item>Проверяется сохранение, загрузка и удаление состояния</item>
///	<item>Проверяется ошибка при недоступном каталоге</item>
///	</list>
/// </remarks>
public sealed class FileStateStoreCrudTests
{
    /// <inheritdoc/>
    public FileStateStoreCrudTests()
    {
    }

    /// <summary>
    ///	Тест 1: Сохраняет состояние при SetAsync
    /// </summary>
    [Fact(DisplayName = "Тест 1: Сохраняет состояние при SetAsync")]
    public async Task Should_SaveState_OnSetAsync()
    {
        // Arrange
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        await using var store = new FileStateStore(new FileStoreOptions { Path = path });
        var ct = CancellationToken.None;

        // Act
        await store.SetAsync("scope", "key", 42, null, ct);

        // Assert
        var file = Path.Combine(path, "scope", "key.json");
        System.IO.File.Exists(file).Should().BeTrue();

        Directory.Delete(path, true);
    }

    /// <summary>
    ///	Тест 2: Загружает сохранённое состояние
    /// </summary>
    [Fact(DisplayName = "Тест 2: Загружает сохранённое состояние")]
    public async Task Should_LoadSavedState()
    {
        // Arrange
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        await using var store = new FileStateStore(new FileStoreOptions { Path = path });
        var ct = CancellationToken.None;
        await store.SetAsync("scope", "key", 7, null, ct);

        // Act
        var value = await store.GetAsync<int?>("scope", "key", ct);

        // Assert
        value.Should().Be(7);

        Directory.Delete(path, true);
    }

    /// <summary>
    ///	Тест 3: Удаляет состояние при RemoveAsync
    /// </summary>
    [Fact(DisplayName = "Тест 3: Удаляет состояние при RemoveAsync")]
    public async Task Should_DeleteState_OnRemoveAsync()
    {
        // Arrange
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        await using var store = new FileStateStore(new FileStoreOptions { Path = path });
        var ct = CancellationToken.None;
        await store.SetAsync("scope", "key", 1, null, ct);

        // Act
        var removed = await store.RemoveAsync("scope", "key", ct);

        // Assert
        removed.Should().BeTrue();
        System.IO.File.Exists(Path.Combine(path, "scope", "key.json")).Should().BeFalse();

        Directory.Delete(path, true);
    }

    /// <summary>
    ///	Тест 4: Бросает исключение при недоступном каталоге
    /// </summary>
    [Fact(DisplayName = "Тест 4: Бросает исключение при недоступном каталоге")]
    public void Should_Throw_When_DirectoryUnavailable()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        var options = new FileStoreOptions { Path = tempFile };

        // Act
        var act = () => new FileStateStore(options);

        // Assert
        act.Should().Throw<IOException>();

        System.IO.File.Delete(tempFile);
    }
}
