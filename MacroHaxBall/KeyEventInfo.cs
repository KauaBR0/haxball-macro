namespace MacroHaxBall;

/// <summary>Dados de um evento de teclado visto pelo hook. Handler pode marcar <see cref="Consume"/> para bloquear a tecla.</summary>
public sealed class KeyEventInfo
{
    public required Vk Vk { get; init; }
    public required uint ScanCode { get; init; }
    public required bool IsDown { get; init; }
    public required bool IsUp { get; init; }
    public required bool IsInjected { get; init; }

    /// <summary>Se true após o handler, o hook não repassa a tecla ao sistema.</summary>
    public bool Consume { get; set; }
}
