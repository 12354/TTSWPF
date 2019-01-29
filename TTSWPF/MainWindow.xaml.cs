using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Speech.Synthesis;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using mrousavy;
using Newtonsoft.Json;
namespace TTSWPF
{
    public class HotkeyTTS
    {
        public Key Key { get; set; }
        public string Text { get; set; }
        public ModifierKeys Modifiers { get; set; }
        [JsonIgnore]
        public HotKey Hotkey { get; set; }
    }
    public partial class MainWindow : Window
    {
        private HotKey focus;
        public MainWindow()
        {
            InitializeComponent();
            speaker = new SpeechSynthesizer();
            speaker.SetOutputToDefaultAudioDevice();
            speaker.Rate = 1;
            speaker.Volume = 100;
            speaker.SelectVoiceByHints(VoiceGender.Female, VoiceAge.Adult);
            

        }
        private Key k;
        private SpeechSynthesizer speaker;
        private void HotkeyKey_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            k = e.Key;
            hotkeyKey.Text = e.Key.ToString();
        }
        private Dictionary<Key,HotkeyTTS> _hotKeys = new Dictionary<Key, HotkeyTTS>();
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var modifier = ModifierKeys.None;
            if (ctrl.IsChecked ?? false)
            {
                modifier |=  ModifierKeys.Control;
            }

            if (alt.IsChecked ?? false)
            {
                modifier |=  ModifierKeys.Alt;
            }

            var text = hotkeyText.Text;
            if (_hotKeys.TryGetValue(k, out var tts))
            {
                tts.Hotkey.Dispose();
            }
            var key = new HotKey(modifier, k,this, hotKey => speaker.SpeakAsync(text));
           
            
            _hotKeys[k] = new HotkeyTTS()
            {
                Key = k,Text = text,
                Modifiers = modifier,
                Hotkey = key
            };
            RefreshList();
            File.WriteAllText("hotkeys.json",JsonConvert.SerializeObject(_hotKeys.Values.ToList()));
            MessageBox.Show("Hotkeys geadded yo");
        }

        private void RefreshList()
        {
            hotkeyList.Clear();
            foreach (var hotkeyTts in _hotKeys)
            {
                hotkeyList.Text += $"{hotkeyTts.Value.Modifiers} {hotkeyTts.Key} : {hotkeyTts.Value.Text}\n";
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
          
        }
        private void PlayBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.B && Keyboard.Modifiers == ModifierKeys.Control)
            {
                speaker.SpeakAsync(playBox.Text);
                playBox.Clear();
            }

            if (e.Key == Key.Return)
            {
                speaker.SpeakAsync(playBox.Text);
                playBox.Clear();
                WindowHelper.BringProcessToFront();
                Thread.Sleep(20);
                WindowHelper.BringProcessToFront();
                
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            focus = new HotKey(ModifierKeys.Control,Key.Return,this,key => BringToFront());
            if (File.Exists("hotkeys.json"))
            {
                var hotkeys =JsonConvert.DeserializeObject< List<HotkeyTTS>>(File.ReadAllText("hotkeys.json"));
                foreach (var hotkeyTts in hotkeys)
                {
                    var key = new HotKey(
                        hotkeyTts.Modifiers,
                        hotkeyTts.Key,
                        this,
                        hotKey => speaker.SpeakAsync(hotkeyTts.Text)
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
                speaker.SpeakAsync(playBox.Text);
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
    public static class WindowHelper
    {
        public static void BringProcessToFront()
        {
            IntPtr handle = csgo;
            if (csgo == IntPtr.Zero)
            {
                return;
            }
            if (IsIconic(handle))
            {
                ShowWindow(handle, SW_RESTORE);
            }

            SetForegroundWindow(handle);
        }

        const int SW_RESTORE = 9;

        [DllImport("User32.dll")]
        private static extern bool SetForegroundWindow(IntPtr handle);
        [DllImport("User32.dll")]
        private static extern bool ShowWindow(IntPtr handle, int nCmdShow);
        [DllImport("User32.dll")]
        private static extern bool IsIconic(IntPtr handle);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        private static IntPtr csgo = IntPtr.Zero;
        public static void SaveForeGround()
        {

            csgo = GetForegroundWindow();
        }
    }
}
