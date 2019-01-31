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
}