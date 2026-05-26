using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FinancyApplication
{
    public class OpenAiService
    {
        private static readonly HttpClient client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        private const string ApiUrl = "https://api.openai.com/v1/chat/completions";
        private const string BuiltInKey = "";

        public static string LoadApiKey()
        {
            string folder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string filePath = Path.Combine(folder, "FinancyApp", "openai_key.txt");

            if (File.Exists(filePath))
            {
                string fileKey = File.ReadAllText(filePath).Trim();
                if (!string.IsNullOrEmpty(fileKey))
                {
                    return fileKey;
                }
            }

            return BuiltInKey;
        }

        public static void SaveApiKey(string key)
        {
            string folder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string dir = Path.Combine(folder, "FinancyApp");
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            File.WriteAllText(Path.Combine(dir, "openai_key.txt"), key.Trim());
        }

        // Sends a message to GPT and returns the reply
        private static async Task<string> AskGpt(string apiKey, string prompt)
        {
            string requestBody = JsonSerializer.Serialize(new
            {
                model = "gpt-4o-mini",
                messages = new[] { new { role = "user", content = prompt } },
                temperature = 0
            });

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, ApiUrl);
            request.Headers.Add("Authorization", "Bearer " + apiKey);
            request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.SendAsync(request);
            string responseText = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("OpenAI API error: " + responseText);
            }

            JsonDocument doc = JsonDocument.Parse(responseText);
            string reply = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return reply;
        }

        public static async Task<Dictionary<string, int>> MapColumns(string[] headers, string apiKey)
        {
            string headerList = string.Join(", ", headers);

            string prompt =
                "I have a bank CSV export with these column headers: " + headerList + "\n\n" +
                "Map each header to one of these field names: Date, Description, Amount, Type, Category.\n" +
                "Reply ONLY with a JSON object using the 0-based column index. Use -1 if no column fits a field.\n" +
                "Example: {\"Date\":0,\"Description\":1,\"Amount\":2,\"Type\":-1,\"Category\":-1}";

            string reply = await AskGpt(apiKey, prompt);

            reply = reply.Trim();
            if (reply.Contains("{"))
            {
                int start = reply.IndexOf('{');
                int end = reply.LastIndexOf('}');
                if (end > start)
                {
                    reply = reply.Substring(start, end - start + 1);
                }
            }

            Dictionary<string, int> result = new Dictionary<string, int>();
            string[] fields = { "Date", "Description", "Amount", "Type", "Category" };

            JsonDocument doc = JsonDocument.Parse(reply);
            foreach (string field in fields)
            {
                if (doc.RootElement.TryGetProperty(field, out JsonElement val))
                {
                    result[field] = val.GetInt32();
                }
                else
                {
                    result[field] = -1;
                }
            }

            return result;
        }

        // Suggests a category for each transaction description using the user's existing categories
        public static async Task<List<string>> SuggestCategories(
            List<string> descriptions,
            List<string> categoryNames,
            string apiKey)
        {
            string catList = string.Join(", ", categoryNames);
            const int batchSize = 20;
            List<string> allResults = new List<string>();

            for (int batchStart = 0; batchStart < descriptions.Count; batchStart += batchSize)
            {
                int batchEnd = Math.Min(batchStart + batchSize, descriptions.Count);
                List<string> batch = descriptions.GetRange(batchStart, batchEnd - batchStart);

                string descList = "";
                for (int i = 0; i < batch.Count; i++)
                {
                    descList += (i + 1) + ". " + batch[i] + "\n";
                }

                string prompt =
                    "You are categorizing bank transactions.\n" +
                    "Available categories — you MUST use one of these EXACTLY as written, do not invent new ones:\n" +
                    catList + "\n\n" +
                    "Rules:\n" +
                    "- Each description also includes its type in brackets, e.g. [Expense] or [Income]. Use this to help pick.\n" +
                    "- If the description looks like a salary, wage or transfer in, prefer an Income category.\n" +
                    "- If unsure, pick the closest match from the list — never leave one empty.\n" +
                    "- Reply ONLY with a valid JSON array, one category name per item, same order as input.\n" +
                    "- Every item in your array must be copied exactly from the available categories list above.\n" +
                    "Example output: [\"Food & Dining\", \"Transport\", \"Salary\"]\n\n" +
                    descList;

                string reply = await AskGpt(apiKey, prompt);

                reply = reply.Trim();
                if (reply.Contains("["))
                {
                    int start = reply.IndexOf('[');
                    int end = reply.LastIndexOf(']');
                    if (end > start)
                    {
                        reply = reply.Substring(start, end - start + 1);
                    }
                }

                JsonDocument doc = JsonDocument.Parse(reply);
                foreach (JsonElement el in doc.RootElement.EnumerateArray())
                {
                    allResults.Add(el.GetString() ?? "");
                }
            }

            return allResults;
        }
    }
}
