using System;
using System.IO;
using UnityEngine;

public static class WavUtility
{
    // Convert audio clip to WAV byte array
    public static byte[] FromAudioClip(AudioClip clip)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            int headerSize = 44;
            int fileSize = clip.samples * clip.channels * 2 + headerSize;
            int sampleRate = clip.frequency;
            int channels = clip.channels;
            short bitsPerSample = 16;

            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                // RIFF header
                writer.Write(System.Text.Encoding.UTF8.GetBytes("RIFF"));
                writer.Write(fileSize - 8); // File size minus RIFF and WAVE headers
                writer.Write(System.Text.Encoding.UTF8.GetBytes("WAVE"));

                // fmt chunk
                writer.Write(System.Text.Encoding.UTF8.GetBytes("fmt "));
                writer.Write(16); // Subchunk1Size (PCM = 16)
                writer.Write((short)1); // Audio format (1 = PCM)
                writer.Write((short)channels);
                writer.Write(sampleRate);
                writer.Write(sampleRate * channels * bitsPerSample / 8);
                writer.Write((short)(channels * bitsPerSample / 8));
                writer.Write(bitsPerSample);

                // data chunk
                writer.Write(System.Text.Encoding.UTF8.GetBytes("data"));
                writer.Write(clip.samples * clip.channels * 2); // Subchunk2Size

                // Write audio data
                float[] samples = new float[clip.samples * clip.channels];
                clip.GetData(samples, 0);
                foreach (var sample in samples)
                {
                    short intSample = (short)(sample * 32767);
                    writer.Write(intSample);
                }
            }
            return stream.ToArray();
        }
    }

    // Save AudioClip as WAV file
    public static void SaveWavFile(string filePath, AudioClip clip)
    {
        byte[] wavData = FromAudioClip(clip);
        File.WriteAllBytes(filePath, wavData);
    }

    // Load WAV file into an AudioClip
    public static AudioClip ToAudioClip(string filePath)
    {
        byte[] fileData = File.ReadAllBytes(filePath);
        return ToAudioClip(fileData);
    }

    // Convert WAV byte array to AudioClip
    public static AudioClip ToAudioClip(byte[] wavData)
    {
        using (MemoryStream stream = new MemoryStream(wavData))
        using (BinaryReader reader = new BinaryReader(stream))
        {
            reader.BaseStream.Seek(22, SeekOrigin.Begin); // Go to number of channels
            short channels = reader.ReadInt16();

            reader.BaseStream.Seek(24, SeekOrigin.Begin); // Go to sample rate
            int sampleRate = reader.ReadInt32();

            reader.BaseStream.Seek(40, SeekOrigin.Begin); // Go to data size
            int dataSize = reader.ReadInt32();

            byte[] audioData = reader.ReadBytes(dataSize);
            float[] samples = new float[audioData.Length / 2];

            for (int i = 0; i < samples.Length; i++)
            {
                short sample = BitConverter.ToInt16(audioData, i * 2);
                samples[i] = sample / 32768f;
            }

            AudioClip clip = AudioClip.Create("LoadedWav", samples.Length / channels, channels, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
