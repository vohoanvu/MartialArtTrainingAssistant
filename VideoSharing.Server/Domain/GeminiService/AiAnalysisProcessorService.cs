using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SharedEntities.Data;
using SharedEntities.Models;
using VideoSharing.Server.Models.Dtos;

namespace VideoSharing.Server.Domain.GeminiService
{
    public class AiAnalysisProcessorService
    {
        private readonly MyDatabaseContext _context;

        public AiAnalysisProcessorService(MyDatabaseContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task ProcessAnalysisJsonAsync(string json, int videoId)
        {
            if (string.IsNullOrEmpty(json))
                throw new ArgumentException("JSON string cannot be null or empty.", nameof(json));

            // Deserialize JSON
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var analysis = JsonSerializer.Deserialize<AiAnalysisResultResponse>(json, options)
                ?? throw new InvalidOperationException("Failed to deserialize JSON.");

            // Dictionary to map technique names to Technique entities
            var techniqueMap = new Dictionary<string, Techniques>();

            // Process techniques_identified
            foreach (var tech in analysis.TechniquesIdentified)
            {
                // Get or create PositionalScenario
                var positionalScenario = await _context.PositionalScenarios
                    .FirstOrDefaultAsync(ps => ps.Name == tech.PositionalScenario);

                if (positionalScenario == null)
                {
                    positionalScenario = new PositionalScenario { Name = tech.PositionalScenario };
                    _context.PositionalScenarios.Add(positionalScenario);
                }

                // Get or create TechniqueType
                var techniqueType = await _context.TechniqueTypes
                    .FirstOrDefaultAsync(tt => tt.Name == tech.TechniqueType && tt.PositionalScenario.Name == tech.PositionalScenario);

                if (techniqueType == null)
                {
                    techniqueType = new TechniqueType { Name = tech.TechniqueType, PositionalScenario = positionalScenario };
                    _context.TechniqueTypes.Add(techniqueType);
                }

                // Get or create Technique
                var technique = await _context.Techniques
                    .FirstOrDefaultAsync(t => t.Name == tech.TechniqueName && t.TechniqueType.Name == tech.TechniqueType);

                if (technique == null)
                {
                    technique = new Techniques
                    {
                        Name = tech.TechniqueName,
                        TechniqueType = techniqueType,
                        Description = tech.Description
                    };
                    _context.Techniques.Add(technique);
                }

                //Get or create AiFeedback
                var aiFeedback = await _context.AiFeedbacks
                    .FirstOrDefaultAsync(f => f.VideoId == videoId && f.Technique.Name == tech.TechniqueName);
                if (aiFeedback == null)
                {
                    aiFeedback = new AiFeedback
                    {
                        VideoId = videoId,
                        Technique = technique,
                        StartTimestamp = TimeSpan.TryParse(tech.StartTimestamp, out var startResult) ? startResult : null,
                        EndTimestamp = TimeSpan.TryParse(tech.EndTimestamp, out var endResult) ? endResult : null,
                    };
                    _context.AiFeedbacks.Add(aiFeedback);
                }

                techniqueMap[tech.TechniqueName] = technique;
            }

            // Process suggested_drills
            foreach (var drillDto in analysis.SuggestedDrills)
            {
                if (techniqueMap.TryGetValue(drillDto.RelatedTechnique, out var technique))
                {
                    // Check if Drill already exists
                    var drill = await _context.Drills
                        .FirstOrDefaultAsync(d => d.Name == drillDto.Name && d.Technique.Name == drillDto.RelatedTechnique);

                    if (drill == null)
                    {
                        drill = new Drills
                        {
                            Name = drillDto.Name,
                            Description = drillDto.Description,
                            Focus = drillDto.Focus,
                            Duration = ParseDuration(drillDto.Duration),
                            Technique = technique
                        };
                        _context.Drills.Add(drill);
                    }
                    else
                    {
                        // Update existing drill
                        drill.Description = drillDto.Description;
                        drill.Focus = drillDto.Focus;
                        drill.Duration = ParseDuration(drillDto.Duration);
                    }
                }
            }

            var existingAiAnalysisResult = await _context.AiAnalysisResults.FirstOrDefaultAsync(a => a.VideoId == videoId);
            if (existingAiAnalysisResult == null)
            {
                existingAiAnalysisResult = new AiAnalysisResult
                {
                    VideoId = videoId,
                    AnalysisJson = json,
                    Strengths = JsonSerializer.Serialize(analysis.Strengths),
                    AreasForImprovement = JsonSerializer.Serialize(analysis.AreasForImprovement),
                };
                _context.AiAnalysisResults.Add(existingAiAnalysisResult);
            }
            else
            {
                existingAiAnalysisResult.AnalysisJson = json;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<TechniqueAnalysisDto> GetAnalysisResultDtoByVideoId(int videoId)
        {
            var analysisDto  = await _context.AiAnalysisResults.Where(a => a.VideoId == videoId && a.Drills != null)
                .Include(a => a.Drills!)
                .ThenInclude(d => d.Technique)
                .ThenInclude(t => t.TechniqueType)
                .ThenInclude(tt => tt.PositionalScenario)
                .Select(a => new TechniqueAnalysisDto
                {
                    Techniques= a.Drills!.Select(d => new TechniqueDto
                    {
                        Name = d.Technique.Name,
                        TechniqueType = new TechniqueTypeDto
                        {
                            Id = d.Technique.TechniqueType.Id,
                            Name = d.Technique.TechniqueType.Name,
                            PositionalScenario = d.Technique.TechniqueType.PositionalScenario.Name,
                        },
                        PositionalScenario = new PositionalScenarioDto
                        {
                            Id = d.Technique.TechniqueType.PositionalScenario.Id,
                            Name = d.Technique.TechniqueType.PositionalScenario.Name,
                        }
                    }).ToList(),
                    Drills = a.Drills!.Select(d => new DrillDto
                    {
                        Name = d.Name,
                        Duration = d.Duration.ToString(),
                        Description = d.Description,
                        RelatedTechniqueName = d.Technique.Name
                    }).ToList(),
                }).FirstOrDefaultAsync();

            if (analysisDto == null)
                throw new InvalidOperationException("No analysis result found that have already processed Drills and Techniques.");

            return analysisDto;
        }

        private TimeSpan ParseDuration(string durationStr)
        {
            if (string.IsNullOrEmpty(durationStr))
                return TimeSpan.Zero;

            // Handle formats like "5 minutes", "3-5 minute rounds", "5-10 minutes"
            try
            {
                var parts = durationStr.ToLower().Split(' ');
                if (parts.Length > 0 && parts.Any(p => p.Contains("minute")))
                {
                    var numberPart = parts[0];
                    if (numberPart.Contains("-"))
                    {
                        // Take the lower bound for ranges like "3-5"
                        var minValue = int.Parse(numberPart.Split('-')[0]);
                        return TimeSpan.FromMinutes(minValue);
                    }
                    else if (int.TryParse(numberPart, out int minutes))
                    {
                        return TimeSpan.FromMinutes(minutes);
                    }
                }
            }
            catch (FormatException)
            {
                // Log parsing failure if needed
            }
            return TimeSpan.Zero; // Default fallback
        }
    }
}