using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace VibranceGoggles
{
    /// <summary>
    /// UI text in 13 languages. Follows the Windows display language unless the user picks one.
    /// To add a language: append its code to <see cref="Codes"/>, its name to <see cref="NativeNames"/>,
    /// and one more column to every row of <see cref="Table"/>.
    /// </summary>
    internal static class S
    {
        public static readonly string[] Codes =
            { "en", "es", "pt", "fr", "de", "it", "ru", "uk", "pl", "tr", "zh", "ja", "ko" };

        public static readonly string[] NativeNames =
            { "English", "Español", "Português", "Français", "Deutsch", "Italiano", "Русский",
              "Українська", "Polski", "Türkçe", "简体中文", "日本語", "한국어" };

        private static int _lang;

        /// <summary>Two-letter code currently in use.</summary>
        public static string Current => Codes[_lang];

        /// <summary>Selects a language; null or empty means "same as Windows".</summary>
        public static void Init(string code)
        {
            if (string.IsNullOrEmpty(code)) code = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            int i = Array.IndexOf(Codes, code);
            _lang = i >= 0 ? i : 0;
        }

        private static string T(string key) => Table[key][_lang];

        public static string AppName => "Vibrance Goggles";

        public static string Graphics => T(nameof(Graphics));
        public static string NoGpu => T(nameof(NoGpu));
        public static string Desktop => T(nameof(Desktop));
        public static string Games => T(nameof(Games));
        public static string Game => T(nameof(Game));
        public static string Vibrance => T(nameof(Vibrance));
        public static string SelectedGame => T(nameof(SelectedGame));
        public static string Add => T(nameof(Add));
        public static string Remove => T(nameof(Remove));
        public static string Options => T(nameof(Options));
        public static string Enabled => T(nameof(Enabled));
        public static string OnlyFocused => T(nameof(OnlyFocused));
        public static string StartWithWindows => T(nameof(StartWithWindows));
        public static string Language => T(nameof(Language));
        public static string LanguageAuto => T(nameof(LanguageAuto));
        public static string Hotkey => T(nameof(Hotkey));
        public static string HotkeyHint => T(nameof(HotkeyHint));
        public static string HotkeyNone => T(nameof(HotkeyNone));
        public static string HotkeyBusy => T(nameof(HotkeyBusy));
        public static string StatusDesktop => T(nameof(StatusDesktop));
        public static string StatusDisabled => T(nameof(StatusDisabled));
        public static string Open => T(nameof(Open));
        public static string Enable => T(nameof(Enable));
        public static string Disable => T(nameof(Disable));
        public static string Exit => T(nameof(Exit));
        public static string TrayHint => T(nameof(TrayHint));
        public static string ToggledOn => T(nameof(ToggledOn));
        public static string ToggledOff => T(nameof(ToggledOff));
        public static string AddTitle => T(nameof(AddTitle));
        public static string AddPrompt => T(nameof(AddPrompt));
        public static string Ok => T(nameof(Ok));
        public static string Cancel => T(nameof(Cancel));
        public static string ResetTip => T(nameof(ResetTip));
        public static string Star => T(nameof(Star));
        public static string GamesHint => T(nameof(GamesHint));
        public static string StartupBlocked => T(nameof(StartupBlocked));

        // Column order = Codes: en, es, pt, fr, de, it, ru, uk, pl, tr, zh, ja, ko
        private static readonly Dictionary<string, string[]> Table = new Dictionary<string, string[]>
        {
            [nameof(Graphics)] = new[] { "Graphics", "Gráfica", "Placa de vídeo", "Carte graphique", "Grafikkarte", "Scheda grafica", "Видеокарта", "Відеокарта", "Karta graficzna", "Ekran kartı", "显卡", "グラフィックス", "그래픽" },
            [nameof(NoGpu)] = new[]
            {
                "No compatible Intel or AMD graphics found. See log.txt for details.",
                "No se encontró una gráfica Intel o AMD compatible. Revisa log.txt.",
                "Nenhuma placa Intel ou AMD compatível encontrada. Veja o log.txt.",
                "Aucune carte graphique Intel ou AMD compatible trouvée. Voir log.txt.",
                "Keine kompatible Intel- oder AMD-Grafik gefunden. Siehe log.txt.",
                "Nessuna scheda grafica Intel o AMD compatibile trovata. Vedi log.txt.",
                "Совместимая видеокарта Intel или AMD не найдена. См. log.txt.",
                "Сумісну відеокарту Intel або AMD не знайдено. Див. log.txt.",
                "Nie znaleziono zgodnej karty Intel ani AMD. Zobacz log.txt.",
                "Uyumlu Intel veya AMD ekran kartı bulunamadı. log.txt dosyasına bakın.",
                "未找到兼容的 Intel 或 AMD 显卡。详情见 log.txt。",
                "対応する Intel または AMD のグラフィックスが見つかりません。log.txt を確認してください。",
                "호환되는 Intel 또는 AMD 그래픽을 찾을 수 없습니다. log.txt를 확인하세요.",
            },
            [nameof(Desktop)] = new[] { "Desktop vibrance", "Saturación en el escritorio", "Saturação na área de trabalho", "Saturation sur le bureau", "Sättigung auf dem Desktop", "Saturazione sul desktop", "Насыщенность на рабочем столе", "Насиченість на робочому столі", "Nasycenie na pulpicie", "Masaüstü doygunluğu", "桌面饱和度", "デスクトップの彩度", "바탕화면 채도" },
            [nameof(Games)] = new[] { "Games", "Juegos", "Jogos", "Jeux", "Spiele", "Giochi", "Игры", "Ігри", "Gry", "Oyunlar", "游戏", "ゲーム", "게임" },
            [nameof(Game)] = new[] { "Game", "Juego", "Jogo", "Jeu", "Spiel", "Gioco", "Игра", "Гра", "Gra", "Oyun", "游戏", "ゲーム", "게임" },
            [nameof(Vibrance)] = new[] { "Vibrance", "Saturación", "Saturação", "Saturation", "Sättigung", "Saturazione", "Насыщенность", "Насиченість", "Nasycenie", "Doygunluk", "饱和度", "彩度", "채도" },
            [nameof(SelectedGame)] = new[] { "Vibrance for selected game", "Saturación del juego seleccionado", "Saturação do jogo selecionado", "Saturation du jeu sélectionné", "Sättigung für das ausgewählte Spiel", "Saturazione del gioco selezionato", "Насыщенность для выбранной игры", "Насиченість для вибраної гри", "Nasycenie dla wybranej gry", "Seçili oyunun doygunluğu", "所选游戏的饱和度", "選択したゲームの彩度", "선택한 게임의 채도" },
            [nameof(Add)] = new[] { "Add game…", "Agregar juego…", "Adicionar jogo…", "Ajouter un jeu…", "Spiel hinzufügen…", "Aggiungi gioco…", "Добавить игру…", "Додати гру…", "Dodaj grę…", "Oyun ekle…", "添加游戏…", "ゲームを追加…", "게임 추가…" },
            [nameof(Remove)] = new[] { "Remove", "Quitar", "Remover", "Supprimer", "Entfernen", "Rimuovi", "Удалить", "Видалити", "Usuń", "Kaldır", "移除", "削除", "제거" },
            [nameof(Options)] = new[] { "Options", "Opciones", "Opções", "Options", "Optionen", "Opzioni", "Параметры", "Параметри", "Opcje", "Seçenekler", "选项", "オプション", "옵션" },
            [nameof(Enabled)] = new[] { "Enabled", "Activado", "Ativado", "Activé", "Aktiviert", "Attivo", "Включено", "Увімкнено", "Włączone", "Etkin", "启用", "有効", "사용" },
            [nameof(OnlyFocused)] = new[] { "Only while the game window is focused", "Solo mientras el juego está en primer plano", "Só enquanto o jogo estiver em primeiro plano", "Seulement quand le jeu est au premier plan", "Nur wenn das Spiel im Vordergrund ist", "Solo quando il gioco è in primo piano", "Только когда игра на переднем плане", "Лише коли гра на передньому плані", "Tylko gdy gra jest na pierwszym planie", "Yalnızca oyun ön plandayken", "仅当游戏在前台时", "ゲームが前面にあるときのみ", "게임이 활성 창일 때만" },
            [nameof(StartWithWindows)] = new[] { "Start with Windows (minimized)", "Iniciar con Windows (minimizado)", "Iniciar com o Windows (minimizado)", "Lancer avec Windows (réduit)", "Mit Windows starten (minimiert)", "Avvia con Windows (ridotto a icona)", "Запускать с Windows (свёрнуто)", "Запускати з Windows (згорнуто)", "Uruchamiaj z Windows (zminimalizowane)", "Windows ile başlat (simge durumunda)", "随 Windows 启动（最小化）", "Windows と同時に起動（最小化）", "Windows 시작 시 실행(최소화)" },
            [nameof(Language)] = new[] { "Language", "Idioma", "Idioma", "Langue", "Sprache", "Lingua", "Язык", "Мова", "Język", "Dil", "语言", "言語", "언어" },
            [nameof(LanguageAuto)] = new[] { "Automatic (Windows)", "Automático (Windows)", "Automático (Windows)", "Automatique (Windows)", "Automatisch (Windows)", "Automatica (Windows)", "Автоматически (Windows)", "Автоматично (Windows)", "Automatycznie (Windows)", "Otomatik (Windows)", "自动（跟随 Windows）", "自動 (Windows)", "자동 (Windows)" },
            [nameof(Hotkey)] = new[] { "On/off hotkey", "Atajo para activar/desactivar", "Atalho para ligar/desligar", "Raccourci marche/arrêt", "Tastenkürzel Ein/Aus", "Scorciatoia on/off", "Горячая клавиша вкл/выкл", "Гаряча клавіша увімк/вимк", "Skrót wł./wył.", "Aç/kapat kısayolu", "开关快捷键", "オン/オフのショートカット", "켜기/끄기 단축키" },
            [nameof(HotkeyHint)] = new[] { "Click and press keys · Backspace clears", "Haz clic y pulsa las teclas · Retroceso borra", "Clique e pressione as teclas · Backspace apaga", "Cliquez puis appuyez sur les touches · Retour arrière efface", "Klicken und Tasten drücken · Rücktaste löscht", "Fai clic e premi i tasti · Backspace cancella", "Щёлкните и нажмите клавиши · Backspace очищает", "Клацніть і натисніть клавіші · Backspace очищає", "Kliknij i naciśnij klawisze · Backspace czyści", "Tıklayıp tuşlara basın · Geri tuşu siler", "点击后按下按键 · Backspace 清除", "クリックしてキーを押す · Backspace で消去", "클릭 후 키를 누르세요 · Backspace로 지우기" },
            [nameof(HotkeyNone)] = new[] { "None", "Ninguno", "Nenhum", "Aucun", "Keins", "Nessuna", "Нет", "Немає", "Brak", "Yok", "无", "なし", "없음" },
            [nameof(HotkeyBusy)] = new[] { "That hotkey is used by another app", "Ese atajo ya lo usa otra aplicación", "Esse atalho já é usado por outro app", "Ce raccourci est déjà utilisé par une autre application", "Dieses Kürzel wird bereits von einer anderen App verwendet", "Questa scorciatoia è già usata da un'altra app", "Это сочетание уже занято другой программой", "Це сполучення вже зайняте іншою програмою", "Ten skrót jest już używany przez inną aplikację", "Bu kısayol başka bir uygulama tarafından kullanılıyor", "该快捷键已被其他程序占用", "このショートカットは他のアプリで使用中です", "이 단축키는 다른 앱에서 사용 중입니다" },
            [nameof(StatusDesktop)] = new[] { "Desktop", "Escritorio", "Área de trabalho", "Bureau", "Desktop", "Desktop", "Рабочий стол", "Робочий стіл", "Pulpit", "Masaüstü", "桌面", "デスクトップ", "바탕화면" },
            [nameof(StatusDisabled)] = new[] { "Disabled", "Desactivado", "Desativado", "Désactivé", "Deaktiviert", "Disattivato", "Выключено", "Вимкнено", "Wyłączone", "Devre dışı", "已禁用", "無効", "사용 안 함" },
            [nameof(Open)] = new[] { "Open", "Abrir", "Abrir", "Ouvrir", "Öffnen", "Apri", "Открыть", "Відкрити", "Otwórz", "Aç", "打开", "開く", "열기" },
            [nameof(Enable)] = new[] { "Enable", "Activar", "Ativar", "Activer", "Aktivieren", "Attiva", "Включить", "Увімкнути", "Włącz", "Etkinleştir", "启用", "有効にする", "사용" },
            [nameof(Disable)] = new[] { "Disable", "Desactivar", "Desativar", "Désactiver", "Deaktivieren", "Disattiva", "Выключить", "Вимкнути", "Wyłącz", "Devre dışı bırak", "禁用", "無効にする", "사용 안 함" },
            [nameof(Exit)] = new[] { "Exit", "Salir", "Sair", "Quitter", "Beenden", "Esci", "Выход", "Вихід", "Zakończ", "Çıkış", "退出", "終了", "종료" },
            [nameof(TrayHint)] = new[]
            {
                "Still running here. Right-click the icon to exit.",
                "Sigo funcionando aquí. Clic derecho en el icono para salir.",
                "Continuo rodando aqui. Clique com o botão direito no ícone para sair.",
                "Toujours actif ici. Clic droit sur l'icône pour quitter.",
                "Läuft hier weiter. Rechtsklick auf das Symbol zum Beenden.",
                "Sono ancora attivo qui. Clic destro sull'icona per uscire.",
                "Программа работает здесь. Щёлкните значок правой кнопкой, чтобы выйти.",
                "Програма працює тут. Клацніть значок правою кнопкою, щоб вийти.",
                "Nadal działam tutaj. Kliknij ikonę prawym przyciskiem, aby zakończyć.",
                "Burada çalışmaya devam ediyorum. Çıkmak için simgeye sağ tıklayın.",
                "仍在此处运行。右键单击图标可退出。",
                "ここで動作中です。終了するにはアイコンを右クリックします。",
                "여기에서 계속 실행 중입니다. 종료하려면 아이콘을 마우스 오른쪽 버튼으로 클릭하세요.",
            },
            [nameof(ToggledOn)] = new[] { "Vibrance enabled", "Saturación activada", "Saturação ativada", "Saturation activée", "Sättigung aktiviert", "Saturazione attivata", "Насыщенность включена", "Насиченість увімкнено", "Nasycenie włączone", "Doygunluk açık", "饱和度已启用", "彩度オン", "채도 켜짐" },
            [nameof(ToggledOff)] = new[] { "Vibrance disabled", "Saturación desactivada", "Saturação desativada", "Saturation désactivée", "Sättigung deaktiviert", "Saturazione disattivata", "Насыщенность выключена", "Насиченість вимкнено", "Nasycenie wyłączone", "Doygunluk kapalı", "饱和度已禁用", "彩度オフ", "채도 꺼짐" },
            [nameof(AddTitle)] = new[] { "Add game", "Agregar juego", "Adicionar jogo", "Ajouter un jeu", "Spiel hinzufügen", "Aggiungi gioco", "Добавить игру", "Додати гру", "Dodaj grę", "Oyun ekle", "添加游戏", "ゲームを追加", "게임 추가" },
            [nameof(AddPrompt)] = new[]
            {
                "Pick a running program or type its .exe name:",
                "Elige un programa abierto o escribe el nombre de su .exe:",
                "Escolha um programa aberto ou digite o nome do .exe:",
                "Choisissez un programme ouvert ou tapez le nom de son .exe :",
                "Wähle ein laufendes Programm oder gib den .exe-Namen ein:",
                "Scegli un programma aperto o scrivi il nome del suo .exe:",
                "Выберите запущенную программу или введите имя .exe:",
                "Виберіть запущену програму або введіть назву .exe:",
                "Wybierz uruchomiony program lub wpisz nazwę pliku .exe:",
                "Çalışan bir program seçin veya .exe adını yazın:",
                "选择正在运行的程序或输入其 .exe 名称：",
                "実行中のプログラムを選ぶか、.exe 名を入力:",
                "실행 중인 프로그램을 선택하거나 .exe 이름을 입력하세요:",
            },
            [nameof(Ok)] = new[] { "OK", "Aceptar", "OK", "OK", "OK", "OK", "ОК", "OK", "OK", "Tamam", "确定", "OK", "확인" },
            [nameof(Cancel)] = new[] { "Cancel", "Cancelar", "Cancelar", "Annuler", "Abbrechen", "Annulla", "Отмена", "Скасувати", "Anuluj", "İptal", "取消", "キャンセル", "취소" },
            [nameof(ResetTip)] = new[] { "Reset to 100%", "Restablecer a 100%", "Restaurar para 100%", "Réinitialiser à 100 %", "Auf 100 % zurücksetzen", "Ripristina a 100%", "Сбросить до 100%", "Скинути до 100%", "Przywróć 100%", "%100'e sıfırla", "重置为 100%", "100% にリセット", "100%로 초기화" },
            [nameof(Star)] = new[] { "Star on GitHub ★", "Danos una ★ en GitHub", "Dê uma ★ no GitHub", "Mettez une ★ sur GitHub", "Auf GitHub mit ★ bewerten", "Lascia una ★ su GitHub", "Поставьте ★ на GitHub", "Поставте ★ на GitHub", "Daj ★ na GitHubie", "GitHub'da ★ verin", "在 GitHub 上点 ★", "GitHub で ★ を付ける", "GitHub에서 ★ 주기" },
            [nameof(StartupBlocked)] = new[]
            {
                "Windows has startup turned off for this app. Turn it on in Settings → Apps → Startup.",
                "Windows tiene desactivado el inicio de esta app. Actívalo en Configuración → Aplicaciones → Inicio.",
                "O Windows desativou a inicialização deste app. Ative em Configurações → Aplicativos → Inicialização.",
                "Windows a désactivé le démarrage de cette app. Activez-le dans Paramètres → Applications → Démarrage.",
                "Windows hat den Autostart dieser App deaktiviert. Aktiviere ihn unter Einstellungen → Apps → Autostart.",
                "Windows ha disattivato l'avvio di questa app. Attivalo in Impostazioni → App → Avvio.",
                "Windows отключила автозапуск этого приложения. Включите его в Параметры → Приложения → Автозагрузка.",
                "Windows вимкнула автозапуск цієї програми. Увімкніть його в Параметри → Програми → Автозавантаження.",
                "Windows wyłączył autostart tej aplikacji. Włącz go w Ustawienia → Aplikacje → Uruchamianie.",
                "Windows bu uygulamanın başlangıçta açılmasını kapattı. Ayarlar → Uygulamalar → Başlangıç'tan açın.",
                "Windows 已关闭此应用的开机启动。请在 设置 → 应用 → 启动 中开启。",
                "Windows でこのアプリのスタートアップが無効になっています。設定 → アプリ → スタートアップ で有効にしてください。",
                "Windows에서 이 앱의 시작 프로그램이 꺼져 있습니다. 설정 → 앱 → 시작 프로그램에서 켜세요.",
            },
            [nameof(GamesHint)] = new[] { "Uncheck a game to pause it", "Desmarca un juego para pausarlo", "Desmarque um jogo para pausá-lo", "Décochez un jeu pour le mettre en pause", "Haken entfernen, um ein Spiel zu pausieren", "Deseleziona un gioco per sospenderlo", "Снимите флажок, чтобы приостановить игру", "Зніміть позначку, щоб призупинити гру", "Odznacz grę, aby ją wstrzymać", "Duraklatmak için oyunun işaretini kaldırın", "取消勾选可暂停该游戏", "チェックを外すとそのゲームを一時停止", "체크를 해제하면 해당 게임을 일시 중지합니다" },
        };

        /// <summary>Catches a missing translation at startup in Debug builds.</summary>
        [System.Diagnostics.Conditional("DEBUG")]
        public static void Validate()
        {
            var bad = Table.Where(kv => kv.Value.Length != Codes.Length).Select(kv => kv.Key).ToList();
            if (bad.Count > 0) throw new InvalidOperationException("Incomplete translations: " + string.Join(", ", bad));
        }
    }
}
