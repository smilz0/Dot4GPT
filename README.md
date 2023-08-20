# Dot 4 GPT
This program works with [Left 4 GPT](https://github.com/smilz0/Left4GPT) to make the L4D2 survivor bots talk with you via chat using ChatGPT.

Here you can see it in action:

https://www.youtube.com/watch?v=as2hVSjyxQs

The program is a **.NET** console application written in **C#** you can easily recompile with **Visual Studio**, if you want.

It must run on the same machine of the L4D2 server, which in case of you hosting the game it's your PC but it can also run on Windows/Linux (tested on Ubuntu Server 22.04) dedicated servers, if your hosting provider allows you to do so. The **.NET 6.0** runtimes must be installed.


### How to run
You need to run one instance of the program for each bot you want to talk. The bot name must be added as command line parameter.
- Windows
```bat
Dot4GPT.exe nick
```
- Linux
```sh
dotnet ./Dot4GPT.dll nick
```

***The user you use to run the program must have r/w access to the L4D2 server's `ems` directory***.

So you may want to use the same user you use to run the L4D2 server.


### Settings
The first time you run the program, it terminates after creating its default settings file `settings.json`, which appears like this:
```json
{"ApiKey":"","IOPath":"C:\\Program Files (x86)\\Steam\\steamapps\\common\\Left 4 Dead 2\\left4dead2\\ems\\left4gpt","MaxTokens":50,"MaxContext":10,"ResetIdleSeconds":90,"APIErrors":false}
```
You must add your OpenAI API Key and change the path to the `ems\left4gpt` folder (if needed), then run it again.

- **ApiKey** (your OpenAI API Key)
- **IOPath** (full path to the `ems\left4gpt` folder)
- **MaxTokens** (max number of tokens used by the API in a single API call)
- **MaxContext** (the program keeps a list of max this amount of latest messages as context for the AI)
- **ResetIdleSeconds** (after how long of no messages to this bot the program will reset the context)
- **APIErrors** (true = if the API call fails, the error message is used as reply. false = the message "Sorry what?" will be used instead)
