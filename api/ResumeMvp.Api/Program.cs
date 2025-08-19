using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();

app.MapPost("/parse", (ParseRequest req) =>
{
    // Stub: return a fake title. We'll make this real in the next step.
    var result = new ParseResponse
    {
        CandidateId = req.CandidateId,
        Sections = new Sections { Titles = new List<string> { "Software Engineer" } }
    };
    return Results.Json(result);
});

app.MapPost("/score", (ScoreRequest req) =>
{
    // Stub: rule match if title contains any keyword (case-insensitive)
    var title = req.Title ?? "";
    var keywords = req.Criteria?.QualifiedIfAnyTitleContains ?? new List<string>();
    var ruleMatch = keywords.Any(k => title.Contains(k, StringComparison.OrdinalIgnoreCase));
    var semanticScore = 0.15; // stub > threshold

    var qualified = ruleMatch || semanticScore >= 0.12;
    return Results.Json(new
    {
        qualified,
        matchedTitle = ruleMatch ? title : null,
        ruleMatch,
        semanticScore,
        rationale = qualified
            ? "Stub: rule or semantic threshold passed."
            : "Stub: neither rule nor semantic threshold passed."
    });
});

app.Run();

record ParseRequest(string CandidateId, string FilePath);
record EmbedRequest(string CandidateId, Dictionary<string, List<string>> TextBlocks);
record ScoreRequest(string CandidateId, string? Title, ScoreCriteria? Criteria);

class ScoreCriteria
{
    [JsonPropertyName("qualifiedIfAnyTitleContains")]
    public List<string> QualifiedIfAnyTitleContains { get; set; } = new();
}

class ParseResponse
{
    public string CandidateId { get; set; } = "";
    public Sections Sections { get; set; } = new();
}

class Sections
{
    public List<string> Titles { get; set; } = new();
    public string? Summary { get; set; }
    public List<string> SkillsRaw { get; set; } = new();
    public List<ExperienceItem> Experience { get; set; } = new();
    public List<EducationItem> Education { get; set; } = new();
    public List<string> Certifications { get; set; } = new();
    public string? Location { get; set; }
}

class ExperienceItem
{
    public string? Title { get; set; }
    public string? Company { get; set; }
    public string? Start { get; set; }
    public string? End { get; set; }
    public string? Text { get; set; }
}

class EducationItem
{
    public string? Degree { get; set; }
    public string? School { get; set; }
    public string? Year { get; set; }
}
