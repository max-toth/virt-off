namespace Game.Scripts.Common
{
    /// <summary>Время для логики, которую нужно тестировать без движка.</summary>
    public interface IClock
    {
        /// <summary>Секунды с запуска, не зависят от Time.timeScale.</summary>
        float UnscaledTime { get; }
    }
}
