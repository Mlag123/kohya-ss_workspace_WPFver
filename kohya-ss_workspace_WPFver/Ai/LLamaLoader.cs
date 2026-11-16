using LLama;
using LLama.Common;
using LLama.Sampling;
using System;
using System.IO;
using LLama.Native;

namespace kohya_ss_workspace_WPFver.Ai;

public class LLamaLoader
{
    
    /// <summary>
    /// THIS TEST AI CODE, FROM LLAMA.CPP
    /// </summary>
    private string modelPath = @"C:\AI\Models\SAMAYA_ZAEBATAYA.gguf";

 public async Task run()
     {
         
         NativeLibraryConfig.All
             .WithCuda(true)
             .WithSearchDirectory(Path.Combine(
                 AppContext.BaseDirectory,
                 "runtimes", "win-x64", "native", "cuda13"))
             .WithVulkan(false)
             .WithLogCallback(delegate (LLamaLogLevel level, string message) {
                 if (level >= LLamaLogLevel.Warning)   // Warning / Error / Fatal
                     Console.WriteLine($"{level}: {message}");
             });
         var parameters = new ModelParams(modelPath)
         {
             ContextSize = 4096, 
             GpuLayerCount = 35 
         };
 
         using var model = LLamaWeights.LoadFromFile(parameters);
         using var context = model.CreateContext(parameters);
         var executor = new InteractiveExecutor(context);
 
         var chatHistory = new ChatHistory();
         chatHistory.AddMessage(AuthorRole.System,
             "Transcript of a dialog, where the User interacts with an Assistant named Bob. Bob is helpful, kind, honest, good at writing, and never fails to answer the User's requests immediately and with precision.");
         chatHistory.AddMessage(AuthorRole.User, "Hello, Bob.");
         chatHistory.AddMessage(AuthorRole.Assistant, "Hello. How may I help you today?");
 
         ChatSession session = new ChatSession(executor, chatHistory);
 
         InferenceParams inferenceParams = new InferenceParams()
         {
             MaxTokens = 256, 
             AntiPrompts = new List<string> { "User:" }, 
             SamplingPipeline = new DefaultSamplingPipeline(),
         };
 
         Console.ForegroundColor = ConsoleColor.Yellow;
         Console.WriteLine("The chat session has started. Type 'exit' to quit.");
 
         while (true)
         {
             Console.ForegroundColor = ConsoleColor.Green;
             Console.Write("User: ");
             string userInput = Console.ReadLine() ?? "";
 
             if (userInput.Trim().ToLower() == "exit")
                 break;
 
             Console.ForegroundColor = ConsoleColor.White;
             Console.Write("Assistant: ");
 
             await foreach (var text in session.ChatAsync(new ChatHistory.Message(AuthorRole.User, userInput), inferenceParams))
             {
                 Console.Write(text);
             }
             
             Console.WriteLine(); // Ебанный перенос строки после ответа
         }
     }
}