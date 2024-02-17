// Replace "YOUR_API_KEY" with your actual API key
using Google.Apis.Auth.OAuth2;
using Google.Cloud.TextToSpeech.V1;
using NAudio.Wave;
using System.Diagnostics;
using System.Runtime.CompilerServices;

internal class Program
{
    private static void Main(string[] args)
    {

        //string firstFilePath = GetAudioFilePath("first.mp3");
        //string secondFilePath = GetAudioFilePath("second.mp3");
        //string mergedFilePath = GetAudioFilePath("merged.mp3");
        //int pauseDurationInSeconds = 3;

        //MergeMp3Files(firstFilePath, secondFilePath, mergedFilePath, pauseDurationInSeconds);

        //Console.WriteLine("Files merged successfully.");
    }

    static void CreateFileFromText()
    {
        TextToSpeechClient client = new TextToSpeechClientBuilder
        {
            CredentialsPath = GetApiKeyFilePath()
        }.Build();

        // The text to be synthesized
        string text = "от имени команды Creation Ministries International благодарю вас за ваш первый визит";

        // Perform the Text-to-Speech request
        SynthesisInput input = new SynthesisInput
        {
            Text = text
        };
        VoiceSelectionParams voice = new VoiceSelectionParams
        {
            LanguageCode = "ru-RU",
            Name = "ru-RU-Wavenet-D",
            SsmlGender = SsmlVoiceGender.Male
        };
        AudioConfig config = new AudioConfig
        {
            AudioEncoding = AudioEncoding.Mp3,
            SpeakingRate = 1.0
        };
        var response = client.SynthesizeSpeech(input, voice, config);

        // Save the audio response to a file
        using (var output = File.Create("output.mp3"))
        {
            response.AudioContent.WriteTo(output);
        }

        using (var mp3File = TagLib.File.Create(GetAudioFilePath("output.mp3")))
        {
            TimeSpan duration = mp3File.Properties.Duration;
            Console.WriteLine($"MP3 file duration: {duration}");
        }

        Console.WriteLine("Speech synthesis complete. Output saved to output mp3.");
    }

    static void MergeMp3Files(string firstFilePath, string secondFilePath, string mergedFilePath, int pauseDurationInSeconds)
    {
        using (var firstFileReader = new Mp3FileReader(firstFilePath))
        using (var secondFileReader = new Mp3FileReader(secondFilePath))
        {
            var waveFormat = new WaveFormat(firstFileReader.WaveFormat.SampleRate, firstFileReader.WaveFormat.BitsPerSample, firstFileReader.WaveFormat.Channels);
            using (var waveOutputStream = new WaveFileWriter(mergedFilePath, waveFormat))
            {
                // Write the first file
                int bytesRead;
                byte[] buffer = new byte[4096];
                while ((bytesRead = firstFileReader.Read(buffer, 0, buffer.Length)) > 0)
                {
                    waveOutputStream.Write(buffer, 0, bytesRead);
                }

                // Add pause between files
                var pauseSamples = pauseDurationInSeconds * waveFormat.SampleRate;
                var silence = new byte[pauseSamples];
                waveOutputStream.Write(silence, 0, silence.Length);

                // Write the second file
                while ((bytesRead = secondFileReader.Read(buffer, 0, buffer.Length)) > 0)
                {
                    waveOutputStream.Write(buffer, 0, bytesRead);
                }
            }
        }
    }
    private static string GetAudioFilePath(string fileName)
    {
        try
        {
            string programDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string apiKeyFileName = fileName;
            return Path.Combine(programDirectory, apiKeyFileName);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while obtaining the file path: {ex.Message}");
            return null;
        }
    }
    private static string GetApiKeyFilePath()
    {
        try
        {
            string programDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string apiKeyFileName = "api-key.json";
            return Path.Combine(programDirectory, apiKeyFileName);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while obtaining the file path: {ex.Message}");
            return null;
        }
    }

}