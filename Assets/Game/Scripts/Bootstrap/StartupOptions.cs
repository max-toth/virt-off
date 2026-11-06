namespace Game.Scripts.Bootstrap
{
    /// <summary>Как сцена начинает игру: со стартового меню (выбор сервера и ника) или сразу с подключения.</summary>
    public sealed class StartupOptions
    {
        public StartupOptions(bool showStartMenu) => ShowStartMenu = showStartMenu;

        /// <summary>Подключение откладывается до кнопки «Зайти». Нет, если в сцене нет меню
        /// или сервер передан аргументом -server.</summary>
        public bool ShowStartMenu { get; }
    }
}
