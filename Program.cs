// Replace "YOUR_API_KEY" with your actual API key
using Google.Apis.Auth.OAuth2;
using Google.Cloud.TextToSpeech.V1;
using NAudio.Wave;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using TitlesConvertor;

internal class Program
{
    private static void Main(string[] args)
    {
        TitleItem item1 = new TitleItem();
        item1.Id = 1;
        item1.Length = 4.68;
        item1.Path = "1.mp3";
        item1.Pause = 0;
        item1.Text = "от имени команды Creation Ministries International благодарю вас за ваш первый визит";

        TitleItem item2 = new TitleItem();
        item2.Id = 2;
        item2.Length = 7.64;
        item2.Path = "2.mp3";
        item2.Pause = 0;
        item2.Text = "на сайт Creation.com. Создание или эволюция, дизайн или время и шанс, это основополагающий вопрос, который";

        TitleItem item3 = new TitleItem();
        item3.Id = 3;
        item3.Length = 4.96;
        item3.Path = "3.mp3";
        item3.Pause = 0;
        item3.Text = "затрагивает каждого человека, и это потому, что то, во что вы верите относительно того, откуда мы пришли, влияет на ваше";

        List<TitleItem> list = new List<TitleItem>();
        list.Add(item1);
        list.Add(item2);
        list.Add(item3);

        foreach (var item in list)
        {
            CreateFileFromText(item);
        }


        string mergedFilePath = "merged.mp3";

        MergeMp3Files(list, mergedFilePath);

    }

    static void CreateFileFromText(TitleItem item, double speakingRate = 1)
    {
        Console.WriteLine("The function is called");
        TextToSpeechClient client = new TextToSpeechClientBuilder
        {
            CredentialsPath = GetApiKeyFilePath()
        }.Build();

        // Perform the Text-to-Speech request
        SynthesisInput input = new SynthesisInput
        {
            Text = item.Text
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
            SpeakingRate = speakingRate
        };
        var response = client.SynthesizeSpeech(input, voice, config);

        // Save the audio response to a file
        using (var output = File.Create($"{item.Id}.mp3"))
        {
            response.AudioContent.WriteTo(output);
        }
        TimeSpan durationFromApi;
        using (var mp3File = TagLib.File.Create(GetAudioFilePath($"{item.Id}.mp3")))
        {
            durationFromApi = mp3File.Properties.Duration;
        }
        TimeSpan shouldBeThisDuration = TimeSpan.FromSeconds(item.Length);

        double additionalSeconds = 0.05;
        TimeSpan negativeDurationFromApi = durationFromApi.Subtract(TimeSpan.FromSeconds(additionalSeconds));
        TimeSpan positiveDurationFromApi = durationFromApi.Add(TimeSpan.FromSeconds(additionalSeconds));

        if (shouldBeThisDuration < negativeDurationFromApi)
        {
            double rate = durationFromApi / shouldBeThisDuration;
            if (speakingRate == 1)
            {

            }
            else
            {
                rate += speakingRate;
                rate--;
            }
            CreateFileFromText(item, rate);
        }
        if (shouldBeThisDuration > positiveDurationFromApi)
        {
            double rate = shouldBeThisDuration / durationFromApi;
            if (speakingRate == 1)
            {

            }
            else
            {
                rate -= speakingRate;
                rate++;
            }

            CreateFileFromText(item, rate);
        }


        Console.WriteLine("Speech synthesis complete. Output saved to output mp3.");
    }

    static void MergeMp3Files(List<TitleItem> fileList, string mergedFilePath)
    {
        if (fileList.Count == 0)
        {
            throw new ArgumentException("File list is empty.");
        }

        using (var firstFileReader = new Mp3FileReader(fileList[0].Path))
        {
            var waveFormat = firstFileReader.WaveFormat;
            using (var waveOutputStream = new WaveFileWriter(mergedFilePath, new WaveFormat(waveFormat.SampleRate, waveFormat.BitsPerSample, waveFormat.Channels)))
            {
                foreach (var item in fileList)
                {
                    string filePath = item.Path;
                    double pauseDurationInSeconds = item.Pause;

                    using (var fileReader = new Mp3FileReader(filePath))
                    {
                        // Write the file
                        int bytesRead;
                        byte[] buffer = new byte[4096];
                        while ((bytesRead = fileReader.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            waveOutputStream.Write(buffer, 0, bytesRead);
                        }

                        // Add pause between files
                        var pauseSamples = (int)(pauseDurationInSeconds * waveFormat.SampleRate);
                        var silence = new byte[pauseSamples];
                        waveOutputStream.Write(silence, 0, silence.Length);
                    }
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