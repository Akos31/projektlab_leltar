using Plugin.Maui.Audio;
using static InventoryApp.Client.Services.ToneGenerator;

namespace InventoryApp.Client.Services;

public enum ScanSound
{
    Success,
    Warning,
    Error
}

// Hangjelzés a beolvasás eredményéről. Kikapcsolható, a beállítás megmarad.
public class SoundService
{
    private const string EnabledKey = "scan_sound_enabled";
    private const double Volume = 0.35;

    private readonly IAudioManager _audio;
    private IAudioPlayer? _current;

    private readonly Dictionary<ScanSound, byte[]> _sounds = new()
    {
        [ScanSound.Success] = CreateWav(Volume, new Tone(1318.5, 70, 15), new Tone(1760, 90)),
        [ScanSound.Warning] = CreateWav(Volume, new Tone(1174.7, 120, 20), new Tone(880, 160)),
        [ScanSound.Error] = CreateWav(Volume, new Tone(311.1, 100, 10), new Tone(233.1, 100, 10), new Tone(311.1, 100, 10), new Tone(233.1, 160))
    };

    public SoundService(IAudioManager audio)
    {
        _audio = audio;
    }

    public bool IsEnabled
    {
        get => Preferences.Default.Get(EnabledKey, true);
        set => Preferences.Default.Set(EnabledKey, value);
    }

    public void Play(ScanSound sound)
    {
        if (!IsEnabled)
            return;

        try
        {
            _current?.Dispose();
            _current = _audio.CreatePlayer(new MemoryStream(_sounds[sound]));
            _current.Play();
        }
        catch (Exception)
        {
        }
    }
}