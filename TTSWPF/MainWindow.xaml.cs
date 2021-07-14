﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Speech.Synthesis;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using CSCore;
using CSCore.MediaFoundation;
using CSCore.SoundOut;
using mrousavy;
using Newtonsoft.Json;

namespace TTSWPF
{
    public partial class MainWindow : Window
    {
        private readonly Dictionary<Key, HotkeyTTS> _hotKeys = new Dictionary<Key, HotkeyTTS>();
        private HotKey _focus;
        private Key _k;
        private readonly List<WaveOutDevice> _outputDevices;

        public MainWindow()
        {
            InitializeComponent();
            if (File.Exists("output.txt"))
            {
                var devices = File.ReadAllLines("output.txt").Select(s => s.Substring(0,31));
                var enumDevices = WaveOutDevice.EnumerateDevices().ToList();
                _outputDevices = devices.Select(dev => enumDevices.FirstOrDefault(waveout =>
                        waveout.Name.ToLowerInvariant().Contains(dev.ToLowerInvariant())))
                    .Where(dev => dev != null).ToList();
            }
            else
            {
                MessageBox.Show("output.txt not found. Check output.example.txt for an example.");
                Environment.Exit(0);
                _outputDevices = new List<WaveOutDevice>();
            }
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

        private void Speak(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;
            try
            {
                Task.Run(() =>
                {
                    try
                    {
                        using (var speechEngine = new SpeechSynthesizer() {Rate = 1, Volume = 100})
                        {
                            using (var stream = new MemoryStream())
                            {
                                speechEngine.SelectVoiceByHints(VoiceGender.Female, VoiceAge.Adult);
                                speechEngine.SetOutputToWaveStream(stream);
                                speechEngine.Speak(text);
                                var data = stream.ToArray();
                                foreach (var waveOutDevice in _outputDevices)
                                {
                                    Task.Run(() =>
                                    {
                                        try
                                        {
                                            using (var newStream = new MemoryStream(data))
                                            using (var waveOut = new WaveOut {Device = waveOutDevice})
                                            using (var waveSource = new MediaFoundationDecoder(newStream))
                                            {
                                                waveOut.Initialize(waveSource);
                                                waveOut.Play();
                                                waveOut.WaitForStopped();
                                            }
                                        }
                                        catch
                                        {
                                            // ignored
                                        }
                                    });
                                }
                            }
                        }
                    }
                    catch
                    {

                    }
                });
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
                    var key = new HotKey(
                        modifierKeys: hotkeyTts.Modifiers,
                        key: hotkeyTts.Key,
                        window: this,
                        onKeyAction: hotKey => Speak(hotkeyTts.Text)
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