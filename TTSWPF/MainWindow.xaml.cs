using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Speech.Synthesis;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using CSCore;
using CSCore.CoreAudioAPI;
using CSCore.MediaFoundation;
using CSCore.SoundOut;
using HarmonyLib;
using mrousavy;
using Newtonsoft.Json;

namespace TTSWPF
{
    [HarmonyPatch]
    class Patch
    {
        public static int Override = -1;
        static MethodBase TargetMethod()
        {
            return AccessTools.Method(AccessTools.TypeByName("System.Speech.Internal.ObjectTokens.SAPICategories"), "DefaultDeviceOut");
        }
        static void Postfix(ref int __result)
        {
            __result = Override;
        }
    }
    public static class Extension
    {
            public static string SafeSubstring(this string input, int startIndex, int length)
    {
        // Todo: Check that startIndex + length does not cause an arithmetic overflow
        if (input.Length >= (startIndex + length))
        {
            return input.Substring(startIndex, length);
        }
        else
        {
            if (input.Length > startIndex)
            {
                return input.Substring(startIndex);
            }
            else
            {
                return string.Empty;
            }
        }
    }
        public static int IndexOf<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {

            var index = 0;
            foreach (var item in source)
            {
                if (predicate.Invoke(item))
                {
                    return index;
                }
                index++;
            }

            return -1;
        }
    }
    public class SelectableOutputDevice
    {
        public SelectableOutputDevice(string name, int index, bool selected)
        {
            Name = name;
            Index = index;
            IsSelected = selected;
        }

        public string Name { get; set; }
        public bool IsSelected { get; set; }
        public int Index { get; set; }
        public override string ToString()
        {
            return Name;
        }
    }
    public partial class MainWindow : Window
    {
        private readonly Dictionary<Key, HotkeyTTS> _hotKeys = new Dictionary<Key, HotkeyTTS>();
        private HotKey _focus;
        private Key _k;
        private List<int> _outputDevices;

        public MainWindow()
        {

            try
            {
                var speechEngine = new SpeechSynthesizer();

                var harmony = new Harmony("ttswpf.12354.org");
                harmony.PatchAll();

                InitializeComponent();
                var enumDevices = WaveOutDevice.EnumerateDevices().ToList();
                if (File.Exists("output.txt"))
                {
                    var devices = File.ReadAllLines("output.txt").Select(s => s.SafeSubstring(0, 31));

                    _outputDevices = devices.Select(dev => enumDevices.IndexOf(waveout =>
                            waveout.Name.ToLowerInvariant().Contains(dev.ToLowerInvariant())))
                        .Where(dev => dev != -1).ToList();
                }
                else
                {
                    
                    _outputDevices = new List<int>();
                }
                _observableOutputDevices = new ObservableCollection<SelectableOutputDevice>(enumDevices.Select((device, index) => new SelectableOutputDevice(device.Name, index, _outputDevices.Contains(index))));
                selectedOutputDevices.ItemsSource = _observableOutputDevices;

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }

        }
        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            _outputDevices = _observableOutputDevices.Where(dev => dev.IsSelected).Select(dev => dev.Index).ToList();
            File.WriteAllText("output.txt", string.Join(Environment.NewLine, _observableOutputDevices.Where(dev => dev.IsSelected).Select(dev => dev.Name)));
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
            var key = new HotKey(modifier, _k, this, hotKey => Speak(text));


            _hotKeys[_k] = new HotkeyTTS
            {
                Key = _k,
                Text = text,
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
                hotkeyList.Text += $"{(hotkeyTts.Value.Modifiers == ModifierKeys.None ? "" : hotkeyTts.Value.Modifiers.ToString())} {hotkeyTts.Key} : {hotkeyTts.Value.Text}\n";
        }
        private bool _isSpeaking = false;
        private string _isSpeakingText = "";
        private ObservableCollection<SelectableOutputDevice> _observableOutputDevices;

        private void Speak(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;
            if(_outputDevices.Count == 0)
            {
                MessageBox.Show("No audio output device selected. Dont forget to select an output device.");
                return;
            }
            try
            {
                if (_isSpeaking && text == _isSpeakingText)
                    return;
                var list = new List<SpeechSynthesizer>();
                foreach (var device in _outputDevices)
                {
                    var speechEngine = new SpeechSynthesizer();
                    speechEngine.SelectVoiceByHints(VoiceGender.Female, VoiceAge.Adult);
                    Patch.Override = device;
                    speechEngine.SetOutputToDefaultAudioDevice();
                    speechEngine.SpeakAsync(new Prompt(text, SynthesisTextFormat.Text));
                }
            }
            catch
            {

            }
        }

        private void PlayBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.B when Keyboard.Modifiers == ModifierKeys.Control:
                    Speak(playBox.Text);
                    playBox.Clear();
                    break;
                case Key.Return:
                    Speak(playBox.Text);
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
            if (!File.Exists("hotkeys.json") && File.Exists("hotkeys.example.json"))
            {
                if (MessageBox.Show("hotkeys.json not found.\nDo you want to use the example hotkeys?",
                    "Use example hotkeys?", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                {
                    File.Move("hotkeys.example.json", "hotkeys.json");
                }
            }

            if (File.Exists("hotkeys.json"))
            {
                var hotkeys = JsonConvert.DeserializeObject<List<HotkeyTTS>>(File.ReadAllText("hotkeys.json"));
                foreach (var hotkeyTts in hotkeys)
                {
                    try
                    {
                        var key = new HotKey(
                            modifierKeys: hotkeyTts.Modifiers,
                            key: hotkeyTts.Key,
                            window: this,
                            onKeyAction: hotKey => Speak(hotkeyTts.Text)
                        );
                        hotkeyTts.Hotkey = key;
                        _hotKeys[hotkeyTts.Key] = hotkeyTts;
                    }
                    catch
                    {
                    }

                }
            }

            RefreshList();
        }

        private void BringToFront()
        {
            if (IsActive)
            {
                Speak(playBox.Text);
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