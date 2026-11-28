using System;
using System.IO;
using WobblyMods.Core;
using Xunit;

public class SaveFilesTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "wobbly-savefiles-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_folder)) Directory.Delete(_folder, true);
    }

    private string Slot(string sub = "a")
    {
        return Path.Combine(Path.Combine(_folder, sub), "house1.txt");
    }

    [Fact]
    public void FirstSaveMakesTheFolderAndNoBackup()
    {
        string path = Slot();
        SaveFiles.WriteWithBackups(path, "one", 3);
        Assert.Equal("one", File.ReadAllText(path));
        Assert.False(File.Exists(SaveFiles.BackupPath(path, 1)));
        Assert.False(File.Exists(path + ".tmp"));
    }

    [Fact]
    public void BackupNamesSitBesideTheSave()
    {
        Assert.Equal(Path.Combine("x", "house1.bak2"), SaveFiles.BackupPath(Path.Combine("x", "house1.txt"), 2));
    }

    [Fact]
    public void KeepsThreeGenerationsNewestFirst()
    {
        string path = Slot();
        foreach (string text in new[] { "one", "two", "three", "four", "five" })
        {
            SaveFiles.WriteWithBackups(path, text, 3);
        }
        Assert.Equal("five", File.ReadAllText(path));
        Assert.Equal("four", File.ReadAllText(SaveFiles.BackupPath(path, 1)));
        Assert.Equal("three", File.ReadAllText(SaveFiles.BackupPath(path, 2)));
        Assert.Equal("two", File.ReadAllText(SaveFiles.BackupPath(path, 3)));
        Assert.False(File.Exists(SaveFiles.BackupPath(path, 4)));
        Assert.False(File.Exists(path + ".tmp"));
    }

    [Fact]
    public void ALeftoverTempFileDoesNotStopTheNextSave()
    {
        string path = Slot();
        SaveFiles.WriteWithBackups(path, "one", 3);
        File.WriteAllText(path + ".tmp", "half a sa");
        SaveFiles.WriteWithBackups(path, "two", 3);
        Assert.Equal("two", File.ReadAllText(path));
        Assert.Equal("one", File.ReadAllText(SaveFiles.BackupPath(path, 1)));
    }

    [Fact]
    public void ZeroGenerationsJustReplaces()
    {
        string path = Slot();
        SaveFiles.WriteWithBackups(path, "one", 0);
        SaveFiles.WriteWithBackups(path, "two", 0);
        Assert.Equal("two", File.ReadAllText(path));
        Assert.False(File.Exists(SaveFiles.BackupPath(path, 1)));
    }

    [Fact]
    public void NewerPicksTheOneThatExists()
    {
        string shared = Slot("shared"), local = Slot("local");
        Assert.Null(SaveFiles.Newer(shared, local));
        Assert.Null(SaveFiles.Newer(null, local));

        SaveFiles.WriteWithBackups(local, "local", 3);
        Assert.Equal(local, SaveFiles.Newer(shared, local));
        Assert.Equal(local, SaveFiles.Newer(null, local));
        Assert.Equal(local, SaveFiles.Newer(local, null));
    }

    [Fact]
    public void NewerPicksTheMoreRecentWhenBothExist()
    {
        string shared = Slot("shared"), local = Slot("local");
        SaveFiles.WriteWithBackups(shared, "shared", 3);
        SaveFiles.WriteWithBackups(local, "local", 3);

        File.SetLastWriteTimeUtc(shared, new DateTime(2026, 11, 20, 10, 0, 0, DateTimeKind.Utc));
        File.SetLastWriteTimeUtc(local, new DateTime(2026, 11, 21, 10, 0, 0, DateTimeKind.Utc));
        Assert.Equal(local, SaveFiles.Newer(shared, local));

        File.SetLastWriteTimeUtc(shared, new DateTime(2026, 11, 22, 10, 0, 0, DateTimeKind.Utc));
        Assert.Equal(shared, SaveFiles.Newer(shared, local));

        // Same time: the first one (the shared folder) wins.
        File.SetLastWriteTimeUtc(local, new DateTime(2026, 11, 22, 10, 0, 0, DateTimeKind.Utc));
        Assert.Equal(shared, SaveFiles.Newer(shared, local));
    }

    [Fact]
    public void NewerTreatsTheSameFileAsOne()
    {
        string path = Slot();
        SaveFiles.WriteWithBackups(path, "one", 3);
        Assert.Equal(path, SaveFiles.Newer(path, path));
    }
}
