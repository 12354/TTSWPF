using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Speech.Synthesis;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using mrousavy;
using Newtonsoft.Json;

namespace TTSWPF
{
    public class HotkeyTTS
    {
        public Key Key { get; set; }
        public string Text { get; set; }
        public ModifierKeys Modifiers { get; set; }
    }
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<HotkeyTTS> _hotkeys = new List<HotkeyTTS>();

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
        List<HotKey> hk = new List<HotKey>();
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
            var key = new HotKey(
                modifier, 
                k,
                this, 
                hotKey => speaker.SpeakAsync(text)
            );
            hk.Add(key);
            _hotkeys.Add(new HotkeyTTS()
            {
                Key = k,Text = text,
                Modifiers = modifier
            });
            File.WriteAllText("hotkeys.json",JsonConvert.SerializeObject(_hotkeys));
            MessageBox.Show("Hotkeys geadded yo");
            
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (File.Exists("hotkeys.json"))
            {
                _hotkeys = JsonConvert.DeserializeObject< List<HotkeyTTS>>(File.ReadAllText("hotkeys.json"));
                foreach (var hotkeyTts in _hotkeys)
                {
                    var key = new HotKey(
                        hotkeyTts.Modifiers,
                        hotkeyTts.Key,
                        this,
                        hotKey => speaker.SpeakAsync(hotkeyTts.Text)
                    );
                    hk.Add(key);
                }
            }

            MessageBox.Show("Hotkeys geladen du pisser");
        }

        private void PlayBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void PlayBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.B && Keyboard.Modifiers == ModifierKeys.Control)
            {
                speaker.SpeakAsync(playBox.Text);
                playBox.Clear();
                
            }
        }
    }
}
