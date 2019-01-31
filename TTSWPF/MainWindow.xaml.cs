using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Speech.Synthesis;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using mrousavy;
using Newtonsoft.Json;

namespace TTSWPF
{
    public partial class MainWindow : Window
    {
        private readonly Dictionary<Key, HotkeyTTS> _hotKeys = new Dictionary<Key, HotkeyTTS>();
        private readonly SpeechSynthesizer _speaker;
        private HotKey _focus;
        private Key _k;

        public MainWindow()
        {
            InitializeComponent();
            _speaker = new SpeechSynthesizer();
            _speaker.SetOutputToDefaultAudioDevice();
            _speaker.Rate = 1;
            _speaker.Volume = 100;
            _speaker.SelectVoiceByHints(VoiceGender.Female, VoiceAge.Adult);
        }

        private void HotkeyKey_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            _k = e.Key;
            hotkeyKey.Text = e.Key.ToString();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var modifier = ModifierKeys.None;
            if (ctrl.IsChecked ?? false) modifier |= ModifierKeys.Control;

            if (alt.IsChecked ?? false) modifier |= ModifierKeys.Alt;

            var text = hotkeyText.Text;
            if (_hotKeys.TryGetValue(_k, out var tts)) tts.Hotkey.Dispose();
            var key = new HotKey(modifier, _k, this, hotKey => _speaker.SpeakAsync(text));


            _hotKeys[_k] = new HotkeyTTS
            {
                Key = _k, Text = text,
                Modifiers = modifier,
                Hotkey = key
            };
            RefreshList();
            File.WriteAllText("hotkeys.json", JsonConvert.SerializeObject(_hotKeys.Values.ToList()));
            MessageBox.Show("Hotkeys geadded yo");
        }

        private void RefreshList()
        {
            hotkeyList.Clear();
            foreach (var hotkeyTts in _hotKeys)
                hotkeyList.Text += $"{hotkeyTts.Value.Modifiers} {hotkeyTts.Key} : {hotkeyTts.Value.Text}\n";
        }

        private void PlayBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.B when Keyboard.Modifiers == ModifierKeys.Control:
                    _speaker.SpeakAsync(playBox.Text);
                    playBox.Clear();
                    break;
                case Key.Return:
                    _speaker.SpeakAsync(playBox.Text);
                    playBox.Clear();
                    WindowHelper.BringProcessToFront();
                    Thread.Sleep(20);
                    WindowHelper.BringProcessToFront();
                    break;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _focus = new HotKey(modifierKeys: ModifierKeys.Control, key: Key.Return, window: this, onKeyAction: key => BringToFront());
            if (File.Exists("hotkeys.json"))
            {
                var hotkeys = JsonConvert.DeserializeObject<List<HotkeyTTS>>(File.ReadAllText("hotkeys.json"));
                foreach (var hotkeyTts in hotkeys)
                {
                    var key = new HotKey(
                        modifierKeys: hotkeyTts.Modifiers,
                        key: hotkeyTts.Key,
                        window: this,
                        onKeyAction: hotKey => _speaker.SpeakAsync(hotkeyTts.Text)
                    );
                    hotkeyTts.Hotkey = key;
                    _hotKeys[hotkeyTts.Key] = hotkeyTts;
                }
            }

            RefreshList();
        }

        private void BringToFront()
        {
            if (IsActive)
            {
                _speaker.SpeakAsync(playBox.Text);
                playBox.Clear();
                WindowHelper.BringProcessToFront();
                Thread.Sleep(20);
                WindowHelper.BringProcessToFront();
                return;
            }

            WindowHelper.SaveForeGround();
            Activate();
            playBox.Focus();
            playBox.Clear();
        }
    }
}