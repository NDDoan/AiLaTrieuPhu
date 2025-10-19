using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AiLaTrieuPhu.Models;

namespace AiLaTrieuPhu.Services
{
    public class SettingsService
    {
        // Lưu file settings.json tại thư mục Saves
        private static readonly string SettingsFilePath = Path.Combine(AppContext.BaseDirectory, "Assets", "Saves", "settings.json");

        public void SaveSettings(AppSettings settings)
        {
            try
            {
                string directory = Path.GetDirectoryName(SettingsFilePath)!;
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(settings, options);
                File.WriteAllText(SettingsFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving settings: {ex.Message}");
            }
        }

        public AppSettings? LoadSettings()
        {
            if (!File.Exists(SettingsFilePath)) return null;

            try
            {
                string json = File.ReadAllText(SettingsFilePath);
                return JsonSerializer.Deserialize<AppSettings>(json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading settings: {ex.Message}");
                return null;
            }
        }
    }
}
