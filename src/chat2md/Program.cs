using System;
using System.IO;
using System.Text;
using System.Text.Json;

public class Program
{
  public static void Main(string[] args)
  {
    string jsonFilePath = args[0];
    string outputDirectory = args[1];

    // Open the JSON file and parse it
    using (FileStream fs = File.OpenRead(jsonFilePath))
    {
      using (JsonDocument document = JsonDocument.Parse(fs))
      {
        int conversationCount = 0;

        foreach (var conversation in document.RootElement.EnumerateArray())
        {
          // Extract the title and create the markdown filename
          string title = conversation.GetProperty("title").GetString();

          // Create a StringBuilder for the markdown content
          StringBuilder markdownContent = new StringBuilder();

          // Add the conversation title as a heading
          markdownContent.AppendLine($"# {title}");
          markdownContent.AppendLine();

          // Parse the "mapping" for messages
          var mapping = conversation.GetProperty("mapping");
          string? conversationUUID = mapping.EnumerateObject().First().Value.GetProperty("id").GetString();
          Console.WriteLine(conversationUUID);

          string markdownFilename = $"{SanitizeFileName(conversationUUID)}.md";

          // Loop through the messages by ID in the mapping
          foreach (var messageElement in mapping.EnumerateObject())
          {
            try
            {
              var message = messageElement.Value.GetProperty("message");
              if (message.ValueKind != JsonValueKind.Null)
              {
                var author = message.GetProperty("author").GetProperty("role").GetString();
                var contentParts = message.GetProperty("content").GetProperty("parts").EnumerateArray();

                // Append author and message content to markdown
                markdownContent.AppendLine($"**{author}:**");

                foreach (var part in contentParts)
                {
                  markdownContent.AppendLine(part.GetString());
                }

                markdownContent.AppendLine(); // Line break between messages
              }
            }
            catch (Exception e)
            {
              markdownContent.AppendLine(e.Message);
            }
          }

          // Write the conversation content to a markdown file
          string markdownFilePath = Path.Combine(outputDirectory, markdownFilename);
          File.WriteAllText(markdownFilePath, markdownContent.ToString());

          conversationCount++;
        }

        Console.WriteLine($"Converted {conversationCount} conversations to markdown.");
      }
    }
  }

  // Method to sanitize the filename (e.g., removing invalid characters)
  public static string SanitizeFileName(string fileName)
  {
    foreach (char c in Path.GetInvalidFileNameChars())
    {
      fileName = fileName.Replace(c, '_');
    }

    return fileName;
  }
}
