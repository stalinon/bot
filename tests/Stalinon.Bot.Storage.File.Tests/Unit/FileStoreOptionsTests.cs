using System.IO;

using FluentAssertions;

using Stalinon.Bot.Storage.File.Options;

using Xunit;

using FileStateStore = Stalinon.Bot.Storage.File.FileStateStore;

namespace Stalinon.Bot.Storage.File.Tests;

/// <summary>
///	Тесты FileStoreOptions: проверка допустимых и недопустимых значений
/// </summary>
/// <remarks>
///	<list type="number">
///	<item>Проверяется создание хранилища с корректными параметрами</item>
///	<item>Проверяются ошибки при неверных значениях</item>
///	</list>
/// </remarks>
public sealed class FileStoreOptionsTests
{
    /// <inheritdoc/>
    public FileStoreOptionsTests()
    {
    }

    /// <summary>
    ///	Тест 1: Создаёт хранилище с корректными значениями
    /// </summary>
    [Fact(DisplayName = "Тест 1: Создаёт хранилище с корректными значениями")]
    public async Task Should_CreateStore_WithValidOptions()
    {
        // Arrange
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var options = new FileStoreOptions
        {
            Path = path,
            BufferHotKeys = true,
            FlushPeriod = TimeSpan.FromMilliseconds(10),
            CleanUpPeriod = TimeSpan.FromMilliseconds(20)
        };

        // Act
        await using var store = new FileStateStore(options);

        // Assert
        Directory.Exists(path).Should().BeTrue();

        Directory.Delete(path, true);
    }

    /// <summary>
    ///	Тест 2: Бросает исключение при отрицательном FlushPeriod
    /// </summary>
    [Fact(DisplayName = "Тест 2: Бросает исключение при отрицательном FlushPeriod")]
    public void Should_Throw_When_FlushPeriodNegative()
    {
        // Arrange
        var options = new FileStoreOptions
        {
            Path = Path.GetTempPath(),
            BufferHotKeys = true,
            FlushPeriod = TimeSpan.FromSeconds(-1)
        };

        // Act
        var act = () => new FileStateStore(options);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    /// <summary>
    ///	Тест 3: Бросает исключение при отрицательном CleanUpPeriod
    /// </summary>
    [Fact(DisplayName = "Тест 3: Бросает исключение при отрицательном CleanUpPeriod")]
    public void Should_Throw_When_CleanUpPeriodNegative()
    {
        // Arrange
        var options = new FileStoreOptions { Path = Path.GetTempPath(), CleanUpPeriod = TimeSpan.FromSeconds(-1) };

        // Act
        var act = () => new FileStateStore(options);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    /// <summary>
    ///	Тест 4: Бросает исключение при пустом пути
    /// </summary>
    [Fact(DisplayName = "Тест 4: Бросает исключение при пустом пути")]
    public void Should_Throw_When_PathIsEmpty()
    {
        // Arrange
        var options = new FileStoreOptions { Path = string.Empty };

        // Act
        var act = () => new FileStateStore(options);

        // Assert
        act.Should().Throw<ArgumentException>();
    }
}
