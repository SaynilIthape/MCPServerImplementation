using System.Text;
using System.Text.Json;

namespace MCPImplementation
{
    public class OllamaService
    {
        private readonly HttpClient _http = new HttpClient();


        private string BuildPrompt(string question, List<ToolDefinition> tools)
        {
            var toolText = string.Join("\n\n", tools.Select(t =>
            {
                var paramsText = string.Join("\n", t.Parameters.Select(p =>
                    $"- {p.Key} ({p.Value.Type}): {p.Value.Description}"
                ));

                return $"""
                    Tool Name: {t.Name}
                    Description: {t.Description}
                    Parameters:
                    {paramsText}
                    """;
            }));

            return $$"""
                    You are an AI assistant with access to tools.

                    Available Tools:
                    {{toolText}}

                    RULES:
                    - Select the correct tool based on the question
                    - If a tool is needed → respond ONLY in JSON
                    - Do NOT explain anything

                    FORMAT:

                    {
                      "tool": "<tool_name>",
                      "arguments": {
                        "<param_name>": "<value>"
                      }
                    }

                    If no tool is needed, answer normally.

                    Question: {{question}}
                    """;
        }


        public async Task<LlmResult> AskWithToolDetection(string question)
        {
            //var prompt = $$"""
            //    You are an AI assistant.

            //    You have access to this tool:

            //    get_discount(product)

            //    RULES:
            //    - If the question is about discount, price, or offer → you MUST call the tool
            //    - DO NOT answer directly in such cases
            //    - Respond ONLY in JSON when calling tool
            //    - Extract product name from the question

            //    FORMAT:

            //    {
            //      "tool": "get_discount",
            //      "arguments": {
            //        "product": "<product_name>"
            //      }
            //    }

            //    If no tool is needed, answer normally.

            //    Question: {{question}}
            //    """;

            var tools = new List<ToolDefinition>
                    {
                        new ToolDefinition
                        {
                            Name = "get_discount",
                            Description = "Get discount for a product",
                            Parameters = new Dictionary<string, ToolParameter>
                            {
                                ["productName"] = new ToolParameter
                                {
                                    Type = "string",
                                    Description = "Name of the product"
                                }
                            }
                        },

                        new ToolDefinition
                        {
                            Name = "get_price",
                            Description = "Get price of a product",
                            Parameters = new Dictionary<string, ToolParameter>
                            {
                                ["productName"] = new ToolParameter
                                {
                                    Type = "string",
                                    Description = "Name of the product"
                                }
                            }
                        }
                    };



            //var prompt = $$"""
            //    You are an AI assistant.

            //    If the question is about discount:
            //    - Respond ONLY with JSON
            //    - Do NOT include any extra text

            //    Output format:

            //    {
            //      "tool": "get_discount",
            //      "arguments": {
            //        "product": "<product>"
            //      }
            //    }

            //    Question: {{question}}
            //    """;

            var prompt = BuildPrompt(question, tools);

            var requestBody = new
            {
                model = "llama3:8b-instruct-q4_0",
                prompt = prompt,
                stream = false
            };

            var res = await _http.PostAsync("http://localhost:11434/api/generate",
                new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json"));

            var json = await res.Content.ReadAsStringAsync();

            var response = JsonSerializer.Deserialize<OllamaResponse>(json);

            return ParseResponse(response.response);
        }

        public async Task<string> GetFinalAnswer(string question, string toolResult)
        {
            var prompt = $"""
            Answer the question using the tool result.

            Question: {question}
            Tool Result: {toolResult}
            """;

            var requestBody = new
            {
                model = "llama3:8b-instruct-q4_0",
                prompt = prompt,
                stream = false
            };

            var res = await _http.PostAsync("http://localhost:11434/api/generate",
                new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json"));

            var json = await res.Content.ReadAsStringAsync();
            var response = JsonSerializer.Deserialize<OllamaResponse>(json);

            return response.response;
        }

        private LlmResult ParseResponse(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new LlmResult { RawResponse = text };

            text = text.Trim();

            // Only parse if it looks like JSON
            //if (!text.StartsWith("{"))
            //{
            //    return new LlmResult { RawResponse = text };
            //}

            try
            {
                var jsonText = ExtractJson(text);

                if (jsonText != null)
                {
                    var doc = JsonDocument.Parse(jsonText);
                    var tool = doc.RootElement.GetProperty("tool").GetString();
                    var product = doc.RootElement
                                     .GetProperty("arguments")
                                     .GetProperty("productName")
                                     .GetString();

                    return new LlmResult
                    {
                        IsToolCall = true,
                        ToolName = tool,
                        Arguments = product
                    };
                }

                return null;                        

               
            }
            catch
            {
                return new LlmResult { RawResponse = text };
            }
        }


        public class OllamaResponse
        {
            public string response { get; set; }
        }

        public class LlmResult
        {
            public bool IsToolCall { get; set; }
            public string ToolName { get; set; }
            public string Arguments { get; set; }
            public string RawResponse { get; set; }
        }

        private string ExtractJson(string text)
        {
            var start = text.IndexOf('{');
            var end = text.LastIndexOf('}');

            if (start >= 0 && end > start)
            {
                return text.Substring(start, end - start + 1);
            }

            return null;
        }

        public class ToolDefinition
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public Dictionary<string, ToolParameter> Parameters { get; set; }
        }

        public class ToolParameter
        {
            public string Type { get; set; }
            public string Description { get; set; }
        }
    }
}