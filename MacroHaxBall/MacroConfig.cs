using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MacroHaxBall;

public enum MatchMode { Vk, ScanCode, Auto }

public sealed class MacroConfig
{
    /// <summary>Tecla gatilho (nome do enum Vk, ex.: "Oem2" = tecla física do /).</summary>
    public string TriggerKey { get; set; } = "Oem2";

    /// <summary>Como casar o gatilho: "Vk", "ScanCode" ou "Auto" (qualquer dos dois).</summary>
    public string TriggerMatch { get; set; } = "Auto";

    /// <summary>Scan code físico da tecla / (0x35 no layout US; cobre ABNT2 no modo Auto).</summary>
    public string TriggerScanCode { get; set; } = "0x35";

    /// <summary>Tecla que liga/desliga o macro sem consumir o evento.</summary>
    public string ToggleKey { get; set; } = "F8";

    /// <summary>Tecla disparada pelo macro.</summary>
    public string FireKey { get; set; } = "X";

    /// <summary>Bloqueia o gatilho de chegar ao app (evita abrir chat/comando com /).</summary>
    public bool ConsumeTrigger { get; set; } = true;

    /// <summary>Aceita teclas sintéticas como gatilho (teste/automação; padrão ignora).</summary>
    public bool AllowInjected { get; set; } = false;

    public bool StartEnabled { get; set; } = true;

    /// <summary>true = quantidade aleatória entre MinCount e MaxCount; false = FixedCount.</summary>
    public bool UseRandomCount { get; set; } = true;

    /// <summary>Segurar o gatilho continua disparando X na taxa máxima (após a rajada inicial).</summary>
    public bool RepeatWhileHeld { get; set; } = true;

    public int MinCount { get; set; } = 2;
    public int MaxCount { get; set; } = 3;
    public int FixedCount { get; set; } = 3;

    /// <summary>Quanto tempo a tecla disparada fica pressionada (ms).</summary>
    public int PressMs { get; set; } = 15;

    /// <summary>Intervalo entre pressionamentos do burst (ms).</summary>
    public int InterKeyDelayMs { get; set; } = 40;

    /// <summary>Loga toda tecla pressionada (ajuda a descobrir o vk do / no seu layout).</summary>
    public bool Verbose { get; set; } = false;

    // ---- resolvidos em Resolve() ----
    [JsonIgnore] public Vk TriggerVk { get; private set; }
    [JsonIgnore] public Vk ToggleVk { get; private set; }
    [JsonIgnore] public Vk FireVk { get; private set; }
    [JsonIgnore] public uint TriggerScan { get; private set; }
    [JsonIgnore] public MatchMode Match { get; private set; }

    private static readonly JsonSerializerOptions ReadOpts = new()
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        PropertyNameCaseInsensitive = true,
    };

    private static readonly JsonSerializerOptions WriteOpts = new() { WriteIndented = true };

    public static MacroConfig LoadOrCreate(string path)
    {
        if (!File.Exists(path))
        {
            var def = new MacroConfig();
            File.WriteAllText(path, JsonSerializer.Serialize(def, WriteOpts));
            return def;
        }
        return JsonSerializer.Deserialize<MacroConfig>(File.ReadAllText(path), ReadOpts) ?? new MacroConfig();
    }

    public void Resolve()
    {
        TriggerVk = ParseKey(TriggerKey, nameof(TriggerKey));
        ToggleVk = ParseKey(ToggleKey, nameof(ToggleKey));
        FireVk = ParseKey(FireKey, nameof(FireKey));
        TriggerScan = ParseHex(TriggerScanCode, nameof(TriggerScanCode));

        Match = TriggerMatch.Trim().ToLowerInvariant() switch
        {
            "vk" => MatchMode.Vk,
            "scancode" => MatchMode.ScanCode,
            "auto" => MatchMode.Auto,
            _ => throw new FormatException($"{nameof(TriggerMatch)} deve ser Vk, ScanCode ou Auto (recebido: '{TriggerMatch}')"),
        };

        if (MinCount < 1) throw new FormatException($"{nameof(MinCount)} deve ser >= 1");
        if (MaxCount < MinCount) throw new FormatException($"{nameof(MaxCount)} deve ser >= {nameof(MinCount)}");
        if (FixedCount < 1) throw new FormatException($"{nameof(FixedCount)} deve ser >= 1");
        if (PressMs < 0) throw new FormatException($"{nameof(PressMs)} deve ser >= 0");
        if (InterKeyDelayMs < 0) throw new FormatException($"{nameof(InterKeyDelayMs)} deve ser >= 0");
    }

    private static Vk ParseKey(string raw, string field)
    {
        if (Enum.TryParse<Vk>(raw, ignoreCase: true, out var vk))
            return vk;
        throw new FormatException($"{field}: tecla desconhecida '{raw}'. Use nomes como Oem2, X, F8, D0..D9, A..Z.");
    }

    private static uint ParseHex(string raw, string field)
    {
        var s = raw.Trim();
        if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            s = s[2..];
        if (uint.TryParse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var v))
            return v;
        throw new FormatException($"{field}: hex inválido '{raw}' (ex.: 0x35).");
    }
}
