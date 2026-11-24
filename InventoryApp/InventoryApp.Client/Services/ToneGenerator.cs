namespace InventoryApp.Client.Services;

// Rövid hangjelzések előállítása WAV formátumban, hangfájlok nélkül
public static class ToneGenerator
{
    private const int SampleRate = 44100;

    public record Tone(double Frequency, int DurationMs, int PauseAfterMs = 0);

    public static byte[] CreateWav(double volume, params Tone[] tones)
    {
        var samples = new List<short>();

        foreach (var tone in tones)
        {
            int count = SampleRate * tone.DurationMs / 1000;
            int fade = Math.Min(count / 4, SampleRate * 8 / 1000);

            for (int i = 0; i < count; i++)
            {
                double envelope = Math.Min(1.0, Math.Min(i, count - 1 - i) / (double)Math.Max(fade, 1));
                double t = i / (double)SampleRate;
                double value = Math.Sin(2 * Math.PI * tone.Frequency * t)
                             + 0.25 * Math.Sin(4 * Math.PI * tone.Frequency * t);
                samples.Add((short)(value / 1.25 * envelope * volume * short.MaxValue));
            }

            samples.AddRange(new short[SampleRate * tone.PauseAfterMs / 1000]);
        }

        return ToWav(samples);
    }

    private static byte[] ToWav(List<short> samples)
    {
        int dataSize = samples.Count * 2;
        using var stream = new MemoryStream(44 + dataSize);
        using var writer = new BinaryWriter(stream);

        writer.Write("RIFF"u8.ToArray());
        writer.Write(36 + dataSize);
        writer.Write("WAVE"u8.ToArray());
        writer.Write("fmt "u8.ToArray());
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)1);
        writer.Write(SampleRate);
        writer.Write(SampleRate * 2);
        writer.Write((short)2);
        writer.Write((short)16);
        writer.Write("data"u8.ToArray());
        writer.Write(dataSize);
        foreach (var sample in samples)
            writer.Write(sample);

        writer.Flush();
        return stream.ToArray();
    }
}