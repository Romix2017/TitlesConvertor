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

        List<TitleItem> list = Parse("captions.sbv");
        MergeItemsWithZeroPause(list);

        foreach (var item in list)
        {
            CreateFileFromText(item);
        }

        string mergedFilePath = "merged.mp3";

        MergeMp3Files(list, mergedFilePath);

    }
    static void MergeItemsWithZeroPause(List<TitleItem> items)
    {
        for (int i = 0; i < items.Count - 1; i++)
        {
            if (items[i].Pause == 0)
            {
                items[i].Text += " " + items[i + 1].Text;
                items[i].Length += items[i + 1].Length;
                items[i].Pause = items[i + 1].Pause;
                // Remove the next item
                items.RemoveAt(i + 1);
                // Decrement the counter to revisit the current index since items shifted after removal
                i--;
            }
        }
    }
    private string[] RemoveEmptyLines(string[] lines)
    {
        string[] res = new string[] { };
        foreach (var item in lines)
        {
            if (string.IsNullOrEmpty(item)) continue;
            res.Prepend(item);
        }
        return res;
    }
    public static List<TitleItem> Parse(string filePath)
    {
        List<TitleItem> titleItems = new List<TitleItem>();
        string[] lines = File.ReadAllLines(filePath);

        double previousEndTime = 0.0;

        for (int i = 0; i < lines.Length; i += 3)
        {
            string timeInfo = lines[i];
            string text = lines[i + 1];

            string[] timeParts = timeInfo.Split(',');
            string[] startTimeParts = timeParts[0].Split(':');
            string[] endTimeParts = timeParts[1].Split(':');

            TimeSpan startTime = TimeSpan.ParseExact(timeParts[0], "h\\:mm\\:ss\\.fff", null);
            TimeSpan endTime = TimeSpan.ParseExact(timeParts[1], "h\\:mm\\:ss\\.fff", null);

            double length = (endTime - startTime).TotalSeconds;

            int id = i / 2 + 1;

            double pause = 0;

            if (i < lines.Length - 3)
            {
                string[] nextTimeParts = lines[i + 3].Split(',');
                TimeSpan nextStartTime = TimeSpan.ParseExact(nextTimeParts[0], "h\\:mm\\:ss\\.fff", null);
                pause = (nextStartTime - endTime).TotalSeconds;
            }

            TitleItem titleItem = new TitleItem
            {
                Id = id,
                FirstItemPause = id == 1 ? startTime.TotalSeconds : 0,
                Length = length,
                Text = text,
                Path = $"{i / 2 + 1}.mp3",
                Pause = pause
            };

            titleItems.Add(titleItem);

            previousEndTime = endTime.TotalSeconds;
        }

        return titleItems;
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
                        if (item.Id == 1)
                        {
                            // Add pause between files
                            var pauseSamples = (int)(pauseDurationInSeconds * waveFormat.SampleRate);
                            var silence = new byte[pauseSamples];
                            waveOutputStream.Write(silence, 0, silence.Length);
                        }
                        // Write the file
                        int bytesRead;
                        byte[] buffer = new byte[4096];
                        while ((bytesRead = fileReader.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            waveOutputStream.Write(buffer, 0, bytesRead);
                        }
                        if (item.Id > 1)
                        {
                            // Add pause between files
                            var pauseSamples = (int)(pauseDurationInSeconds * waveFormat.SampleRate);
                            var silence = new byte[pauseSamples];
                            waveOutputStream.Write(silence, 0, silence.Length);
                        }
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