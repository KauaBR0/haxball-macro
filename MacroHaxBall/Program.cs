using MacroHaxBall;
using System.Threading;

var cfgPath = Path.Combine(AppContext.BaseDirectory, "config.json");

MacroConfig cfg;
try
{
    cfg = MacroConfig.LoadOrCreate(cfgPath);
    cfg.Resolve();
}
catch (Exception ex)
{
    Console.Error.WriteLine($"[erro] config inválida: {ex.Message}");
    return 1;
}

Console.Title = "MacroHaxBall";
string countDesc = cfg.UseRandomCount
    ? $"{cfg.MinCount}–{cfg.MaxCount} (aleatório)"
    : cfg.FixedCount.ToString();
Console.WriteLine("MacroHaxBall — gatilho dispara a tecla de chute");
Console.WriteLine($"  gatilho: {cfg.TriggerVk} (match: {cfg.Match}, scan 0x{cfg.TriggerScan:X2})");
Console.WriteLine($"  disparo: {countDesc}x {cfg.FireVk} | hold {cfg.PressMs} ms | intervalo {cfg.InterKeyDelayMs} ms");
Console.WriteLine($"  hold contínuo: {(cfg.RepeatWhileHeld ? $"sim — segurar dispara {cfg.FireVk} na taxa máxima" : "não")}");
Console.WriteLine($"  toggle: {cfg.ToggleVk} | consumir gatilho: {cfg.ConsumeTrigger} | injetadas: {(cfg.AllowInjected ? "aceitas" : "ignoradas")}");
Console.WriteLine($"  config: {cfgPath}");
Console.WriteLine("Ctrl+C para sair.");

var sender = new KeySender();
using var worker = new MacroWorker();   // disposed por último (depois do hook parar)
using var hook = new KeyboardHook();

int enabledFlag = cfg.StartEnabled ? 1 : 0; // 1 = macro ligado (lido pelo worker via Volatile)
int holdFlag = 0;                           // 1 = gatilho segurado
uint? armedScan = null;                     // gatilho pressionado aguardando keyup
var rnd = Random.Shared;

hook.KeyEvent += e =>
{
    if (e.IsInjected && !cfg.AllowInjected)
        return;

    if (e.IsDown)
    {
        if (e.Vk == cfg.ToggleVk)
        {
            enabledFlag = enabledFlag == 1 ? 0 : 1;
            Console.WriteLine($"[macro] {(enabledFlag == 1 ? "ATIVADO" : "PAUSADO")} via {cfg.ToggleVk}");
            return; // toggle nunca é consumido
        }

        if (cfg.Verbose)
            Console.WriteLine($"[tecla] {(e.IsInjected ? "injetada " : "")}down {e.Vk} (vk 0x{(int)e.Vk:X2}, scan 0x{e.ScanCode:X2})");

        bool isTrigger = cfg.Match switch
        {
            MatchMode.Vk => e.Vk == cfg.TriggerVk,
            MatchMode.ScanCode => e.ScanCode == cfg.TriggerScan,
            _ => e.Vk == cfg.TriggerVk || e.ScanCode == cfg.TriggerScan,
        };

        if (armedScan is not null)
        {
            // gatilho já segurado: consome auto-repeats para não vazar "/" pro jogo
            if (cfg.ConsumeTrigger && isTrigger && e.ScanCode == armedScan)
                e.Consume = true;
            return;
        }

        if (enabledFlag != 1 || !isTrigger)
            return;

        armedScan = e.ScanCode;
        if (cfg.ConsumeTrigger)
            e.Consume = true;

        if (cfg.RepeatWhileHeld)
        {
            Volatile.Write(ref holdFlag, 1);
            Console.WriteLine($"[macro] gatilho {e.Vk} -> HOLD contínuo de {cfg.FireVk}");
            worker.Enqueue(HoldLoop);
        }
        else
        {
            int count = cfg.UseRandomCount ? rnd.Next(cfg.MinCount, cfg.MaxCount + 1) : cfg.FixedCount;
            Console.WriteLine($"[macro] gatilho {e.Vk} -> {count}x {cfg.FireVk}");
            worker.Enqueue(() => sender.Burst(cfg.FireVk, count, cfg.PressMs, cfg.InterKeyDelayMs));
        }
    }
    else if (e.IsUp)
    {
        if (cfg.Verbose)
            Console.WriteLine($"[tecla] {(e.IsInjected ? "injetada " : "")}up {e.Vk} (vk 0x{(int)e.Vk:X2}, scan 0x{e.ScanCode:X2})");

        if (armedScan is not null && e.ScanCode == armedScan)
        {
            armedScan = null;
            Volatile.Write(ref holdFlag, 0);
        }
    }
};

void HoldLoop()
{
    // Rajada inicial (mesma do toque): garante 2–3 X mesmo em toque rápido.
    int count = cfg.UseRandomCount ? rnd.Next(cfg.MinCount, cfg.MaxCount + 1) : cfg.FixedCount;
    sender.Burst(cfg.FireVk, count, cfg.PressMs, cfg.InterKeyDelayMs);

    // Depois: contínuo na taxa máxima enquanto o gatilho estiver segurado.
    while (Volatile.Read(ref holdFlag) == 1 && Volatile.Read(ref enabledFlag) == 1)
    {
        sender.Press(cfg.FireVk, cfg.PressMs);
        if (cfg.InterKeyDelayMs > 0)
            Thread.Sleep(cfg.InterKeyDelayMs);
    }
}

try
{
    hook.Start();
}
catch (Exception ex)
{
    Console.Error.WriteLine($"[erro] {ex.Message}");
    return 1;
}

Console.WriteLine("[hook] ativo — pronto.");

var exit = new ManualResetEventSlim(false);
Console.CancelKeyPress += (_, a) =>
{
    a.Cancel = true;
    exit.Set();
};
exit.Wait();
Console.WriteLine("Encerrando...");
return 0;
