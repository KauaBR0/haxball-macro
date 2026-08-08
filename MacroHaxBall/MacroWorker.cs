using System.Collections.Concurrent;

namespace MacroHaxBall;

/// <summary>
/// Fila serializada em thread dedicada. Os bursts (com Thread.Sleep entre teclas)
/// rodam aqui — nunca no callback do hook, que tem timeout do Windows.
/// </summary>
public sealed class MacroWorker : IDisposable
{
    private readonly BlockingCollection<Action> _queue = new();
    private readonly Thread _thread;

    public MacroWorker()
    {
        _thread = new Thread(() =>
        {
            foreach (var work in _queue.GetConsumingEnumerable())
            {
                try
                {
                    work();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[erro] burst: {ex.Message}");
                }
            }
        })
        { Name = "macro-worker", IsBackground = true };
        _thread.Start();
    }

    public void Enqueue(Action work)
    {
        if (_queue.IsAddingCompleted)
            return;
        try
        {
            _queue.TryAdd(work);
        }
        catch (InvalidOperationException)
        {
            // CompleteAdding corrido com o Enqueue — ignorar.
        }
    }

    public void Dispose()
    {
        _queue.CompleteAdding();
        _thread.Join(TimeSpan.FromSeconds(2));
        _queue.Dispose();
    }
}
