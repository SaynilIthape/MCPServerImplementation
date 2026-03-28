using MCPImplementation;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
//    app.UseSwagger();
//    app.UseSwaggerUI();
//}


app.MapPost("/ask", async (HttpContext context) =>
{
    var request = await context.Request.ReadFromJsonAsync<AskRequest>();

    if (string.IsNullOrWhiteSpace(request?.Question))
        return Results.BadRequest("Question is required");

    var llm = new OllamaService();

    // Step 1: Ask LLM
    var llmResponse = await llm.AskWithToolDetection(request.Question);

    // Step 2: Check if tool call
    if (llmResponse.IsToolCall)
    {
        var toolResult = ToolHandler.Execute(llmResponse.ToolName, llmResponse.Arguments);

        // Step 3: Send tool result back to LLM
        var finalAnswer = await llm.GetFinalAnswer(request.Question, toolResult);

        return Results.Ok(new { response = finalAnswer });
    }

    return Results.Ok(new { response = llmResponse.RawResponse });
});




app.UseHttpsRedirection();

//var summaries = new[]
//{
//    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
//};

//app.MapGet("/weatherforecast", () =>
//{
//    var forecast = Enumerable.Range(1, 5).Select(index =>
//        new WeatherForecast
//        (
//            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
//            Random.Shared.Next(-20, 55),
//            summaries[Random.Shared.Next(summaries.Length)]
//        ))
//        .ToArray();
//    return forecast;
//})
//.WithName("GetWeatherForecast")
//.WithOpenApi();

app.Run();

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

record AskRequest(string Question);