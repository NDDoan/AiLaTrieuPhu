using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Media;
using System.Windows.Media;

namespace AiLaTrieuPhu.Services
{
    public class AudioService
    {
        private MediaPlayer _sfxPlayer = new MediaPlayer();
        private MediaPlayer _bgmPlayer = new MediaPlayer();

        public double _currentSFXVolume { get; private set; } = 0.5;
        public double _currentMusicVolume { get; private set; } = 0.5;

        public AudioService()
        {
            // Thêm trình bắt lỗi để biết TẠI SAO file không phát được
            _sfxPlayer.MediaFailed += (s, e) =>
                System.Diagnostics.Debug.WriteLine($"[LỖI SFX] {e.ErrorException.Message}");
            _bgmPlayer.MediaFailed += (s, e) =>
                System.Diagnostics.Debug.WriteLine($"[LỖI BGM] {e.ErrorException.Message}");
        }

        public void SetSFXVolume(int volume)
        {
            // Chuyển từ 0-100 sang 0.0-1.0
            _currentSFXVolume = volume / 100.0;
            _sfxPlayer.Volume = _currentSFXVolume;
        }

        public void SetMusicVolume(int volume)
        {
            _currentMusicVolume = volume / 100.0;
            _sfxPlayer.Volume = _currentMusicVolume;
        }

        // Phát âm thanh hiệu ứng (SFX) từ đường dẫn tương đối
        public void PlaySFX(string relativePath)
        {
            try
            {
                // Chuẩn hóa relativePath: Chuyển hết '/' thành '\' trên Windows
                string normalizedSubPath = relativePath.Replace('/', System.IO.Path.DirectorySeparatorChar);
                // Kết hợp đường dẫn từ Assets/Audio/
                string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Audio", normalizedSubPath);

                //System.Diagnostics.Debug.WriteLine($"[KIỂM TRA] Đường dẫn thực tế: {fullPath}");
                if (File.Exists(fullPath))
                {
                    _sfxPlayer.Stop();
                    _sfxPlayer.Open(new Uri(fullPath, UriKind.Absolute));
                    _sfxPlayer.Volume = _currentSFXVolume;
                    _sfxPlayer.Play();
                    //System.Diagnostics.Debug.WriteLine($"[PHÁT SFX] {relativePath} | Vol: {_currentSFXVolume}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Không tìm thấy tệp: {fullPath}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi khi phát SFX: {ex.Message}");
            }
        }

        public void PlayBGM(string relativePath, bool loop = true)
        {
            string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Audio", relativePath);
            if (File.Exists(fullPath))
            {
                _bgmPlayer.Open(new Uri(fullPath, UriKind.Absolute));
                if (loop) _bgmPlayer.MediaEnded += (s, e) => { _bgmPlayer.Position = TimeSpan.Zero; _bgmPlayer.Play(); };
                _bgmPlayer.Volume = _currentMusicVolume;
                _bgmPlayer.Play();
            }
        }

        public void StopBGM() => _bgmPlayer.Stop();
        public void StopSFX() => _sfxPlayer.Stop();
    }
}
