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
        private MediaPlayer _vaPlayer = new MediaPlayer();

        public double _currentSFXVolume { get; private set; } = 0.5;
        public double _currentMusicVolume { get; private set; } = 0.5;
        public double _currentVAVolume { get; private set; } = 0.5;

        private string _currentBgmName = "";

        public event EventHandler VAFinished;

        private TaskCompletionSource<bool> _vaTcs;

        public AudioService()
        {
            // Thêm trình bắt lỗi để biết TẠI SAO file không phát được
            _sfxPlayer.MediaFailed += (s, e) =>
                System.Diagnostics.Debug.WriteLine($"[LỖI SFX] {e.ErrorException.Message}");
            _bgmPlayer.MediaFailed += (s, e) =>
                System.Diagnostics.Debug.WriteLine($"[LỖI BGM] {e.ErrorException.Message}");
            
            _vaPlayer.MediaFailed += (s, e) =>
            {
                System.Diagnostics.Debug.WriteLine($"[LỖI VA] {e.ErrorException.Message}");
                if (_vaTcs != null && !_vaTcs.Task.IsCompleted)
                {
                    _vaTcs.TrySetResult(false);
                }
            };
            
            _vaPlayer.MediaEnded += (s, e) =>
            {
                VAFinished?.Invoke(this, EventArgs.Empty);
                if (_vaTcs != null && !_vaTcs.Task.IsCompleted)
                {
                    _vaTcs.TrySetResult(true);
                }
            };
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
            _bgmPlayer.Volume = _currentMusicVolume;
        }

        public void SetVAVolume(int volume)
        {
            _currentVAVolume = volume / 100.0;
            _vaPlayer.Volume = _currentVAVolume;
        }

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

        public async Task PlaySFXAsync(string relativePath)
        {
            try
            {
                string normalizedSubPath = relativePath.Replace('/', System.IO.Path.DirectorySeparatorChar);
                string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Audio", normalizedSubPath);

                if (File.Exists(fullPath))
                {
                    var tcs = new TaskCompletionSource<bool>();
                    EventHandler handler = null;
                    handler = (s, e) =>
                    {
                        _sfxPlayer.MediaEnded -= handler;
                        tcs.TrySetResult(true);
                    };

                    EventHandler<ExceptionEventArgs> failedHandler = null;
                    failedHandler = (s, e) =>
                    {
                        _sfxPlayer.MediaFailed -= failedHandler;
                        tcs.TrySetResult(false);
                    };

                    _sfxPlayer.MediaEnded += handler;
                    _sfxPlayer.MediaFailed += failedHandler;

                    _sfxPlayer.Stop();
                    _sfxPlayer.Open(new Uri(fullPath, UriKind.Absolute));
                    _sfxPlayer.Volume = _currentSFXVolume;
                    _sfxPlayer.Play();

                    await tcs.Task;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Không tìm thấy tệp: {fullPath}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi khi phát SFXAsync: {ex.Message}");
            }
        }

        public void PlayBGM(string relativePath, bool loop = true)
        {
            // Nếu bài nhạc này đang phát rồi thì không phát lại từ đầu
            if (_currentBgmName == relativePath) return;

            string normalizedSubPath = relativePath.Replace('/', Path.DirectorySeparatorChar);
            string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Audio", normalizedSubPath);

            if (File.Exists(fullPath))
            {
                _bgmPlayer.Stop();
                _bgmPlayer.Open(new Uri(fullPath, UriKind.Absolute));

                // Gỡ sự kiện cũ trước khi đăng ký mới để tránh chồng chéo
                _bgmPlayer.MediaEnded -= OnBgmEnded;
                if (loop)
                {
                    _bgmPlayer.MediaEnded += OnBgmEnded;
                }

                _bgmPlayer.Volume = _currentMusicVolume;
                _bgmPlayer.Play();
                _currentBgmName = relativePath;
            }
        }

        public void PlayVA(string relativePath)
        {
            try
            {
                string normalizedSubPath = relativePath.Replace('/', Path.DirectorySeparatorChar);
                string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Audio", normalizedSubPath);
                if (File.Exists(fullPath))
                {
                    _vaPlayer.Stop();
                    _vaPlayer.Open(new Uri(fullPath, UriKind.Absolute));
                    _vaPlayer.Volume = _currentVAVolume;
                    _vaPlayer.Play();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Không tìm thấy tệp VA: {fullPath}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi khi phát VA: {ex.Message}");
            }
        }

        public async Task PlayVAAsync(string relativePath)
        {
            string normalizedSubPath = relativePath.Replace('/', Path.DirectorySeparatorChar);
            string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Audio", normalizedSubPath);

            // Cancel any ongoing VA playback task
            if (_vaTcs != null && !_vaTcs.Task.IsCompleted)
            {
                _vaTcs.TrySetResult(false);
            }

            if (!File.Exists(fullPath))
            {
                System.Diagnostics.Debug.WriteLine($"Không tìm thấy tệp VA: {fullPath}");
                return;
            }

            _vaTcs = new TaskCompletionSource<bool>();
            PlayVA(relativePath);

            await _vaTcs.Task;
        }

        public async Task PlayVAWithDucking(string path)
        {
            double originalBgmVolume = _bgmPlayer.Volume;
            _bgmPlayer.Volume = originalBgmVolume * 0.3; // Giảm nhạc nền

            await PlayVAAsync(path);

            _bgmPlayer.Volume = originalBgmVolume;
        }

        private void OnBgmEnded(object? sender, EventArgs e)
        {
            _bgmPlayer.Position = TimeSpan.Zero;
            _bgmPlayer.Play();
        }

        public void StopBGM()
        {
            _bgmPlayer.Stop();
            _currentBgmName = "";
        }
        public void StopSFX() => _sfxPlayer.Stop();
        public void StopVA() 
        {
            _vaPlayer.Stop();
            if (_vaTcs != null && !_vaTcs.Task.IsCompleted)
            {
                _vaTcs.TrySetResult(false);
            }
        }
    }
}
