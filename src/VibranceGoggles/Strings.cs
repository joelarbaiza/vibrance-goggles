using System.Globalization;

namespace VibranceGoggles
{
    /// <summary>UI text. Spanish when Windows is in Spanish, English otherwise.</summary>
    internal static class S
    {
        private static readonly bool Es = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "es";

        private static string T(string en, string es) => Es ? es : en;

        public static string AppName => "Vibrance Goggles";

        public static string Graphics => T("Graphics", "Gráfica");
        public static string NoGpu => T("No compatible Intel or AMD graphics found. See log.txt for details.",
                                        "No se encontró una gráfica Intel o AMD compatible. Revisa log.txt.");

        public static string Desktop => T("Desktop vibrance", "Saturación en el escritorio");
        public static string Games => T("Games", "Juegos");
        public static string Game => T("Game", "Juego");
        public static string Vibrance => T("Vibrance", "Saturación");
        public static string SelectedGame => T("Vibrance for selected game", "Saturación del juego seleccionado");
        public static string Add => T("Add game…", "Agregar juego…");
        public static string Remove => T("Remove", "Quitar");

        public static string Options => T("Options", "Opciones");
        public static string Enabled => T("Enabled", "Activado");
        public static string OnlyFocused => T("Only while the game window is focused", "Solo mientras el juego está en primer plano");
        public static string StartWithWindows => T("Start with Windows (minimized)", "Iniciar con Windows (minimizado)");

        public static string Hotkey => T("On/off hotkey", "Atajo para activar/desactivar");
        public static string HotkeyHint => T("Click and press keys · Backspace clears", "Haz clic y pulsa las teclas · Retroceso borra");
        public static string HotkeyNone => T("None", "Ninguno");
        public static string HotkeyBusy => T("That hotkey is used by another app", "Ese atajo ya lo usa otra aplicación");

        public static string StatusDesktop => T("Desktop", "Escritorio");
        public static string StatusDisabled => T("Disabled", "Desactivado");

        public static string Open => T("Open", "Abrir");
        public static string Enable => T("Enable", "Activar");
        public static string Disable => T("Disable", "Desactivar");
        public static string Exit => T("Exit", "Salir");

        public static string TrayHint => T("Still running here. Right-click the icon to exit.",
                                           "Sigo funcionando aquí. Clic derecho en el icono para salir.");
        public static string ToggledOn => T("Vibrance enabled", "Saturación activada");
        public static string ToggledOff => T("Vibrance disabled", "Saturación desactivada");

        public static string AddTitle => T("Add game", "Agregar juego");
        public static string AddPrompt => T("Pick a running program or type its .exe name:",
                                            "Elige un programa abierto o escribe el nombre de su .exe:");
        public static string Ok => T("OK", "Aceptar");
        public static string Cancel => T("Cancel", "Cancelar");
    }
}
