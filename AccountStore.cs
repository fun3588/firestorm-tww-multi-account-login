using System.Text.Json;

namespace logingui;

public sealed class AccountStore
{
    private static readonly string LegacyDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "logingui");
    private static readonly string Dir = InitializeDataDirectory();

    public static string DataDirectory => Dir;
    public static string AccountsPath => Path.Combine(Dir, "accounts.json");
    public static string SettingsPath => Path.Combine(Dir, "settings.json");
    public static string DebugLogPath => Path.Combine(LegacyDir, "debug.log");

    private static string InitializeDataDirectory()
    {
        // An explicit in-process override lets the test harness use isolated data without reading real accounts.
        if (AppContext.GetData("logingui.DataDirectory") is string isolated)
        {
            Directory.CreateDirectory(isolated);
            return isolated;
        }
        // Builds anywhere below logingui share the same versioned data directory.
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "logingui.csproj")))
            directory = directory.Parent;
        var destination = directory != null
            ? Path.Combine(directory.FullName, "data")
            : LegacyDir;
        Directory.CreateDirectory(destination);
        if (!destination.Equals(LegacyDir, StringComparison.OrdinalIgnoreCase))
        {
            foreach (var name in new[] { "accounts.json", "settings.json", "tokens.json" })
            {
                var source = Path.Combine(LegacyDir, name);
                var target = Path.Combine(destination, name);
                if (!File.Exists(target) && File.Exists(source)) File.Copy(source, target);
            }
        }
        return destination;
    }

    private static readonly JsonSerializerOptions Opts = new() { WriteIndented = true };

    public static List<Account> Load()
    {
        using var lease = DataFileLock.Acquire();
        if (!File.Exists(AccountsPath)) return new();
        try
        {
            return JsonSerializer.Deserialize<List<Account>>(File.ReadAllText(AccountsPath))
                ?? throw new InvalidDataException(UiText.Get("账号文件内容无效。"));
        }
        catch (Exception ex)
        {
            throw new IOException(UiText.Get("无法读取账号文件，已保留原文件，请检查: ") + AccountsPath, ex);
        }
    }

    internal static void WriteAtomic(string path, string content)
    {
        using var lease = DataFileLock.Acquire();
        Directory.CreateDirectory(Dir);
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporary, content);
            File.Move(temporary, path, true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    public static void Save(List<Account> accounts) =>
        WriteAtomic(AccountsPath, JsonSerializer.Serialize(accounts, Opts));
    public static AppSettings LoadSettings()
    {
        using var lease = DataFileLock.Acquire();
        try
        {
            if (File.Exists(SettingsPath))
                return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(SettingsPath)) ?? new();
        }
        catch { }
        return new AppSettings();
    }

    public static void SaveSettings(AppSettings s)
    {
        Directory.CreateDirectory(Dir);
        WriteAtomic(SettingsPath, JsonSerializer.Serialize(s, Opts));
    }

    public static void Log(string msg)
    {
        try
        {
            Directory.CreateDirectory(LegacyDir);
            File.AppendAllText(DebugLogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {msg}{Environment.NewLine}");
        }
        catch { }
    }
}

internal sealed class DataFileLock : IDisposable
{
    private readonly Mutex _mutex;
    private DataFileLock(Mutex mutex) { _mutex = mutex; }
    public static DataFileLock Acquire()
    {
        var mutex = new Mutex(false, @"Local\logingui-data-lock");
        try { mutex.WaitOne(); return new(mutex); }
        catch (AbandonedMutexException) { return new(mutex); }
    }
    public void Dispose() { try { _mutex.ReleaseMutex(); } catch { } _mutex.Dispose(); }
}

