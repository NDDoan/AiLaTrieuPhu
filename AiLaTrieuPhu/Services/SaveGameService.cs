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
    public class SaveGameService
    {
        private static readonly string SaveFilePath = Path.Combine(AppContext.BaseDirectory, "Assets", "Saves", "save.json");

        public async Task SaveGameAsync(GameStatus status)
        {
            string directory = Path.GetDirectoryName(SaveFilePath)!; // Dùng '!' vì ta biết nó sẽ không null
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(status, options);
            await File.WriteAllTextAsync(SaveFilePath, json);
        }

        public async Task<GameStatus?> LoadGameAsync()
        {
            if (!File.Exists(SaveFilePath)) return null;

            try
            {
                // Dùng await để đọc file bất đồng bộ
                string json = await File.ReadAllTextAsync(SaveFilePath);

                // Dùng Task.Run để Deserialize (phép toán nặng) bất đồng bộ
                return await Task.Run(() => JsonSerializer.Deserialize<GameStatus>(json));
            }
            catch (Exception)
            {
                return null;
            }
        }

        public bool HasSaveFile() => File.Exists(SaveFilePath);
    }
}
