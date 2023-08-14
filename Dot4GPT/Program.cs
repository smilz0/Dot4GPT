using OpenAI.GPT3;
using OpenAI.GPT3.Managers;
using OpenAI.GPT3.ObjectModels.RequestModels;
using OpenAI.GPT3.ObjectModels;
using Newtonsoft.Json;

// Name of the settings file
const string SettingsFileName = @"settings.json";

if (args.Length == 0)
{
    // Must add the bot's name in the command line
    System.Console.WriteLine("Please enter the survivor bot name");
    return 1;
}

// Read the settings file or create a default one if not exists
Settings? settings = new Settings();

string settingsFilePath = AddTrailingSeparator(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location)) + SettingsFileName;
if (!File.Exists(settingsFilePath))
{
    try
    {
        string json = JsonConvert.SerializeObject(settings);
        System.IO.File.WriteAllText(settingsFilePath, json);

        Console.WriteLine("A new settings file has been created");
    }
    catch (Exception e)
    {
        Console.WriteLine("Error creating a new settings file: " + e.ToString());
        return 1;
    }

    Console.WriteLine("Please, edit the file adding your ChatGPT Api Key and try again");
    return 1;
}

try
{
    settings = JsonConvert.DeserializeObject<Settings>(System.IO.File.ReadAllText(settingsFilePath));
}
catch (Exception e)
{
    Console.WriteLine("Error loading the settings file: " + e.ToString());
    return 1;
}

if (settings == null)
{
    Console.WriteLine("Error loading the settings file");
    return 1;
}

if (string.IsNullOrEmpty(settings.ApiKey))
{
    Console.WriteLine("Api Key is not set. Please, edit the settings file adding your ChatGPT Api Key and try again");
    return 1;
}

Console.WriteLine("Settings file successfully loaded");

// Initialize the OpenAI service with the Api Key
OpenAIService openAiService = new OpenAIService(new OpenAiOptions()
{
    // We should probably find a better way to store our api key than a json file
    // ApiKey = Environment.GetEnvironmentVariable("MY_OPEN_AI_API_KEY")
    ApiKey = settings.ApiKey
});

//serviceCollection.AddOpenAIService();

// Full path to the 'left4dead2/ems/left4gpt' folder
string ioPath = AddTrailingSeparator(settings.IOPath);

// Bot's character name
string character = args[0].ToLower();

// This file will be written by the VScript with the chat message from the player
string inFile = ioPath + character + "_in.txt";

// This file will contain
string outFile = ioPath + character + "_out.txt";

// System level message for the AI model. This is always sent on top of the message list and it's meant to instruct the AI model on how to act
string system = "You are " + Capitalize(character) + " from Left 4 Dead 2 and you must answer questions in 30 words or less.";

// List with the latest (settings.MaxContext) user messages used for context
// This list will always start with the 'system' message and will keep a list of successful message/reply pairs
List<ChatMessage> Messages = new List<ChatMessage>();

// Last time we received a chat message
DateTime lastInteraction = DateTime.Now;

Console.WriteLine("inFile: " + inFile);
Console.WriteLine("outFile: " + outFile);
Console.WriteLine("system: " + system);

Reset();

// Main loop
while (true)
{
    if (File.Exists(inFile))
    {
        string inText = File.ReadAllText(inFile);
        if (!string.IsNullOrEmpty(inText))
        {
            // We received a chat message

            // Update the last interaction time
            lastInteraction = DateTime.Now;

            // Truncate the IN file
            File.WriteAllText(inFile, "");
            //File.Delete(inFile);

            Console.WriteLine("User: " + inText);

            // Add the player's message to the context list
            Messages.Add(ChatMessage.FromUser(inText));

            /* For testing
            string? outText = Console.ReadLine();
            if (string.IsNullOrEmpty(outText))
                outText = "Response error";
            */

            // Create the ChatCompletion to send to the AI
            var completionResult = await openAiService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
            {
                Messages = Messages, // Messages list for context
                Model = Models.ChatGpt3_5Turbo, // AI model
                //Model = Models.TextDavinciV2,
                MaxTokens = settings.MaxTokens // Optional
            });

            string outText = "Sorry what?"; // If, for some reason, the API call fails, the bot will reply with this message (or with the API error if settings.APIErrors is true)
            if (completionResult.Successful)
                outText = completionResult.Choices.First().Message.Content; // Get the first result
            else if (settings.APIErrors)
            {
                if (completionResult.Error != null && completionResult.Error.Message != null)
                    outText = completionResult.Error.Message;
                else if (completionResult.Error != null && completionResult.Error.Code != null)
                    outText = completionResult.Error.Code.ToString();
            }

            Console.WriteLine("Assistant: " + outText);

            if (completionResult.Successful)
                Messages.Add(ChatMessage.FromAssistant(outText)); // If the API call was successfull, add the received reply to the context list
            else
                Messages.RemoveAt(Messages.Count - 1); // Otherwise remove the corresponding player's message

            if (Messages.Count > settings.MaxContext)
            {
                // If the list is full, remove the oldest message/reply pair
                Messages.RemoveAt(1);
                Messages.RemoveAt(1);
            }

            // Write the AI reply to the OUT file
            File.WriteAllText(outFile, outText);
        }

        TimeSpan ts = DateTime.Now - lastInteraction;
        if (ts.TotalSeconds >= settings.ResetIdleSeconds && Messages.Count > 1)
        {
            // If we haven't received any message for (settings.ResetIdleSeconds) seconds then reset the context list
            Console.WriteLine("Reset");
            Reset();
        }
    }
    
    Thread.Sleep(100);
}

string AddTrailingSeparator(string path)
{
    string separator1 = Path.DirectorySeparatorChar.ToString();
    string separator2 = Path.AltDirectorySeparatorChar.ToString();

    path = path.TrimEnd();

    if (path.EndsWith(separator1) || path.EndsWith(separator2))
        return path;

    if (path.Contains(separator2))
        return path + separator2;
    else
        return path + separator1;
}

// Reset the context list
void Reset()
{
    Messages.Clear();
    Messages.Add(ChatMessage.FromSystem(system));
}

string Capitalize(string text)
{
    if (string.IsNullOrEmpty(text))
        return text;

    if (text.Length == 1)
        return "" + char.ToUpper(text[0]);
    else
        return "" + char.ToUpper(text[0]) + text.Substring(1);
}

// Settings file structure
public class Settings
{
    public string ApiKey;
    public string IOPath;
    public int MaxTokens;
    public int MaxContext;
    public int ResetIdleSeconds;
    public bool APIErrors;

    public Settings()
    {
        ApiKey = "";
        IOPath = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\Left 4 Dead 2\\left4dead2\\ems\\left4gpt";
        MaxTokens = 50;
        MaxContext = 10;
        ResetIdleSeconds = 90;
        APIErrors = false;
    }
}
