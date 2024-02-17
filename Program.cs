// Replace "YOUR_API_KEY" with your actual API key
using Google.Apis.Auth.OAuth2;
using Google.Cloud.TextToSpeech.V1;
using System.Runtime.CompilerServices;

internal class Program
{
    private static void Main(string[] args)
    {

        TextToSpeechClient client = new TextToSpeechClientBuilder
        {
            CredentialsPath = GetApiKeyFilePath()
        }.Build();

        // The text to be synthesized
        string text = "Hello, world!";

        // Perform the Text-to-Speech request
        SynthesisInput input = new SynthesisInput
        {
            Text = text
        };
        VoiceSelectionParams voice = new VoiceSelectionParams
        {
            LanguageCode = "en-US",
            SsmlGender = SsmlVoiceGender.Male
        };
        AudioConfig config = new AudioConfig
        {
            AudioEncoding = AudioEncoding.Mp3
        };
        var response = client.SynthesizeSpeech(input, voice, config);

        // Save the audio response to a file
        using (var output = File.Create("output.mp3"))
        {
            response.AudioContent.WriteTo(output);
        }

        Console.WriteLine("Speech synthesis complete. Output saved to output.wav.");
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