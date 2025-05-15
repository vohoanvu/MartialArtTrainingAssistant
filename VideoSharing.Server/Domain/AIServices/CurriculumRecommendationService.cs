using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SharedEntities.Data;
using SharedEntities.Models;
using VideoSharing.Server.Domain.GeminiService;

public class CurriculumRecommendationService(MyDatabaseContext context, IGeminiVisionService geminiService)
{
    private readonly MyDatabaseContext _context = context;
    private readonly IGeminiVisionService _geminiService = geminiService;

    public async Task<CurriculumResponse> GenerateCurriculumAsync(int sessionId)
    {
        // Step 1: Retrieve checked-in students
        var studentUsers = await _context.TrainingSessionFighterJoints
            .Where(tsf => tsf.TrainingSessionId == sessionId)
            .Join(_context.Fighters,
                    tsf => tsf.FighterId,
                    f => f.Id,
                    (tsf, f) => f)
            .Join(_context.Users,
                    f => f.Id,
                    u => u.FighterId,
                    (f, u) => u)
            .ToListAsync();
        var studentIds = studentUsers.Select(u => u.Id).ToList();
        var studentFighters = studentUsers.Select(u => u.Fighter!).ToList();

        // Step 2: Fetch latest video analysis for each student
        var analysisTasks = studentIds.Select(studentId =>
            _context.AiAnalysisResults
                .Where(a => a.Video.UserId == studentId && a.Video.Type == VideoType.StudentUpload)
                .OrderByDescending(a => a.GeneratedAt)
                .FirstOrDefaultAsync()
        ).ToList();
        var latestAnalyses = (await Task.WhenAll(analysisTasks)).Where(a => a != null).ToList();

        // Step 3: Aggregate weaknesses
        var studentAnalysisIds = latestAnalyses.Select(a => a.Id).ToList();
        var weaknessCounts = await _context.AnalysisWeaknesses
            .Where(aw => studentAnalysisIds.Contains(aw.AiAnalysisResultId))
            .GroupBy(aw => aw.WeaknessCategory)
            .Select(g => new { WeaknessCategory = g.Key, Count = g.Count() })
            .OrderByDescending(g => g.Count)
            .Take(5)
            .ToListAsync();
        var topWeaknessCategories = weaknessCounts.Select(w => w.WeaknessCategory.Name).ToList();

        // Step 4: Generate curriculum structure
        // Call the Gemini Chat API
        var trainingSession = await _context.TrainingSessions.FindAsync(sessionId) 
            ?? throw new BadHttpRequestException("This Training Session Id is invalid.");
        var response = await _geminiService.SuggestClassCurriculum(topWeaknessCategories, studentFighters, trainingSession);
        string curriculumJson = response.CurriculumJson;

        // Parse and save
        var curriculum = JsonSerializer.Deserialize<CurriculumResponse>(curriculumJson)
            ?? throw new JsonException("The Chat Response JSON parsing returned null.");

        //(Optional) Step 7: Save recommended drills to TrainingSession
        return curriculum;
    }
}

// private string BuildCurriculumPrompt(List<string> weaknesses, List<Fighter>? students)
// {
//     var studentDetails = students?.Select(s => 
//         $"- Student: Belt Rank - {s.BelkRank}, Height - {s.Height}, Weight - {s.Weight} lbs, Training Experience - {s.Experience.ToString()}"
//     ).Aggregate((a, b) => $"{a}\n{b}");

//     return $"You are an expert martial art instructor. Design a 60-minute Brazilian Jiu-Jitsu (BJJ) class session curriculum based on the following information::\n" +
//         $"- **Most Common Weakness**: {weaknesses}\n" +
//         $"- **Students**:\n{studentDetails}\n" +
//         "Create a curriculum with a 5-minute warm-up, 3-5 main drills targeting the weakness, " +
//         "and a 5-minute cool-down. Return it as a JSON object.";
// }