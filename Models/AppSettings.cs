using System;
 
namespace RimWorldTranslationTool.Models
{
    public class AppSettings
    {
        public string GamePath { get; set; } = "";
        public string GameVersion { get; set; } = "";
        public string Language { get; set; } = "zh-TW";
        public string ModsConfigPath { get; set; } = "";
    }
}
