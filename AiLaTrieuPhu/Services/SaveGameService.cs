    using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AiLaTrieuPhu.Models;
using AiLaTrieuPhu.Utilities;

namespace AiLaTrieuPhu.Services
{
    public class SaveGameService
    {
        private static readonly string SaveFilePath = Path.Combine(AppContext.BaseDirectory, "Assets", "Saves", "save.json");
        // Key và IV dùng cho mã hóa AES (Phải là 16 bytes/128 bits)
        private static readonly byte[] EncryptionKey = Encoding.UTF8.GetBytes("YourSecretKey123"); // 16 ký tự
        private static readonly byte[] EncryptionIV = Encoding.UTF8.GetBytes("IVforGameSave123");  // 16 ký tự
        public async Task SaveGameAsync(GameStatus status)
        {
            string directory = Path.GetDirectoryName(SaveFilePath)!; // Dùng '!' vì ta biết nó sẽ không null
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(status, options);

            // BƯỚC MỚI: MÃ HÓA CHUỖI JSON
            byte[] encryptedData = EncryptStringToBytes_Aes(json, EncryptionKey, EncryptionIV);

            // LƯU DỮ LIỆU ĐÃ MÃ HÓA (dạng byte array)
            await File.WriteAllBytesAsync(SaveFilePath, encryptedData);
        }

        public async Task<GameStatus?> LoadGameAsync()
        {
            if (!File.Exists(SaveFilePath))
            {
                return null; // Không có file save
            }

            try
            {
                // ĐỌC DỮ LIỆU ĐÃ MÃ HÓA (dạng byte array)
                byte[] encryptedData = await File.ReadAllBytesAsync(SaveFilePath);

                // GIẢI MÃ CHUỖI JSON
                string json = DecryptStringFromBytes_Aes(encryptedData, EncryptionKey, EncryptionIV);

                // Deserialize từ JSON đã giải mã
                var loadedGameStatus = JsonSerializer.Deserialize<GameStatus>(json);

                if (loadedGameStatus == null)
                {
                    // Lỗi deserialize không rõ nguyên nhân, xóa file và trả về null
                    DeleteSaveGame();
                    return null;
                }

                // KIỂM TRA CHÍNH SÁCH SAVE MỚI: CHỈ ĐƯỢC TIẾP TỤC GAME ĐANG IN_PROGRESS
                if (loadedGameStatus.CompletionStatus == GameCompletionStatus.InProgress)
                {
                    // Game đang dở, cho phép tiếp tục
                    return loadedGameStatus;
                }
                else
                {
                    // Game đã Thua (Lost) hoặc Từ Bỏ (Quit), ván chơi này đã kết thúc.
                    // Xóa file save để vô hiệu hóa nút "Tiếp tục" trong Menu
                    DeleteSaveGame();
                    return null;
                }
            }
            catch (Exception ex)
            {
                // Xử lý lỗi (ví dụ: file bị hỏng, lỗi đọc/ghi)
                Console.WriteLine($"Error loading save game (Decryption failed): {ex.Message}");
                DeleteSaveGame(); // Xóa file hỏng
                return null;
            }
        }

        public bool HasSaveFile() => File.Exists(SaveFilePath);

        public static void DeleteSaveGame()
        {
            if (File.Exists(SaveFilePath))
            {
                File.Delete(SaveFilePath);
            }
        }

        private static byte[] EncryptStringToBytes_Aes(string plainText, byte[] Key, byte[] IV)
        {
            // Kiểm tra tham số
            if (plainText == null || plainText.Length <= 0)
                throw new ArgumentNullException(nameof(plainText));
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException(nameof(Key));
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException(nameof(IV));

            byte[] encrypted;

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Key;
                aesAlg.IV = IV;

                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            // Ghi tất cả dữ liệu vào stream
                            swEncrypt.Write(plainText);
                        }
                        encrypted = msEncrypt.ToArray();
                    }
                }
            }
            return encrypted;
        }

        private static string DecryptStringFromBytes_Aes(byte[] cipherText, byte[] Key, byte[] IV)
        {
            // Kiểm tra tham số
            if (cipherText == null || cipherText.Length <= 0)
                throw new ArgumentNullException(nameof(cipherText));
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException(nameof(Key));
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException(nameof(IV));

            string plaintext = string.Empty;

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Key;
                aesAlg.IV = IV;

                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msDecrypt = new MemoryStream(cipherText))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {
                            // Đọc tất cả byte đã giải mã từ stream
                            plaintext = srDecrypt.ReadToEnd();
                        }
                    }
                }
            }
            return plaintext;
        }
    }
}
