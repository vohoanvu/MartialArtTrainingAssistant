using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SharedEntities.Data;
using SharedEntities.Models;
using VideoSharing.Server.Models.Dtos;

namespace VideoSharing.Server.Domain.GeminiService
{
    public class AiAnalysisProcessorService(MyDatabaseContext context)
    {
        private readonly MyDatabaseContext _context = context ?? throw new ArgumentNullException(nameof(context));

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

            // Get or create AiAnalysisResult
            var existingAiAnalysisResult = await _context.AiAnalysisResults
                .FirstOrDefaultAsync(a => a.VideoId == videoId);
            if (existingAiAnalysisResult == null)
            {
                existingAiAnalysisResult = new AiAnalysisResult
                {
                    VideoId = videoId,
                    AnalysisJson = json,
                    Strengths = JsonSerializer.Serialize(analysis.Strengths),
                    AreasForImprovement = JsonSerializer.Serialize(analysis.AreasForImprovement),
                    OverallDescription = analysis.OverallDescription,
                    Techniques = new List<Techniques>(),
                    Drills = new List<Drills>()
                };
                _context.AiAnalysisResults.Add(existingAiAnalysisResult);
            }
            else
            {
                existingAiAnalysisResult.AnalysisJson = json;
                existingAiAnalysisResult.Strengths = JsonSerializer.Serialize(analysis.Strengths);
                existingAiAnalysisResult.AreasForImprovement = JsonSerializer.Serialize(analysis.AreasForImprovement);
                existingAiAnalysisResult.OverallDescription = analysis.OverallDescription;
                existingAiAnalysisResult.Techniques.Clear();
                existingAiAnalysisResult.Drills.Clear();
            }

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

                // Retrieve or create TechniqueType
                var techniqueType = await _context.TechniqueTypes
                    .FirstOrDefaultAsync(tt => tt.Name == tech.TechniqueType)
                    ?? new TechniqueType { Name = tech.TechniqueType };

                // Retrieve or create Technique
                var technique = await _context.Techniques
                    .FirstOrDefaultAsync(t => t.Name == tech.TechniqueName && t.TechniqueType.Name == tech.TechniqueType)
                    ?? new Techniques
                    {
                        Name = tech.TechniqueName,
                        Description = tech.Description,
                        TechniqueType = techniqueType
                    };

                // Explicitly link the technique to the current AiAnalysisResult
                technique.AiAnalysisResult = existingAiAnalysisResult;

                // Add technique to AiAnalysisResult
                if (!existingAiAnalysisResult.Techniques.Any(t => t.Name == tech.TechniqueName && t.TechniqueType.Name == tech.TechniqueType))
                {
                    existingAiAnalysisResult.Techniques.Add(technique);
                }

                // Get or create AiFeedback
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
                        existingAiAnalysisResult.Drills.Add(drill);
                    }
                    else
                    {
                        // Update existing drill
                        drill.Description = drillDto.Description;
                        drill.Focus = drillDto.Focus;
                        drill.Duration = ParseDuration(drillDto.Duration);
                        if (!existingAiAnalysisResult.Drills.Any(d => d.Id == drill.Id))
                        {
                            existingAiAnalysisResult.Drills.Add(drill);
                        }
                    }
                }
                else
                {
                    //Just in case the AI model forgot to specify the related Technique, associate that drill with the default generic technique
                    var genericTechnique = await _context.Techniques.FirstAsync(t => t.Name == "Generic Technique");
                    var drill = new Drills
                    {
                        Name = drillDto.Name,
                        Description = drillDto.Description,
                        Focus = drillDto.Focus,
                        Duration = ParseDuration(drillDto.Duration),
                        TechniqueId = genericTechnique.Id
                    };
                    _context.Drills.Add(drill);
                    existingAiAnalysisResult.Drills.Add(drill);
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task<AnalysisResultDto> GetAnalysisResultDtoByVideoId(int videoId)
        {
            var analysisResult = await _context.AiAnalysisResults
                .Where(a => a.VideoId == videoId)
                .Include(a => a.Techniques)
                    .ThenInclude(t => t.TechniqueType)
                        .ThenInclude(tt => tt.PositionalScenario)
                .Include(a => a.Drills)
                    .ThenInclude(d => d.Technique)
                .AsSplitQuery()
                .FirstOrDefaultAsync();

            if (analysisResult == null)
                throw new InvalidOperationException("No analysis result found.");

            // Fetch AiFeedback records for the video
            var feedbackRecords = await _context.AiFeedbacks
                .Where(f => f.VideoId == videoId)
                .ToListAsync();

            var analysisDto = new AnalysisResultDto
            {
                Id = analysisResult.Id,
                Techniques = analysisResult.Techniques.Select(t => new TechniqueDto
                {
                    Id = t.Id,
                    Name = t.Name,
                    Description = t.Description,
                    TechniqueType = new TechniqueTypeDto
                    {
                        Id = t.TechniqueType.Id,
                        Name = t.TechniqueType.Name,
                        PositionalScenario = t.TechniqueType.PositionalScenario.Name
                    },
                    PositionalScenario = new PositionalScenarioDto
                    {
                        Id = t.TechniqueType.PositionalScenario.Id,
                        Name = t.TechniqueType.PositionalScenario.Name
                    },
                    StartTimestamp = feedbackRecords.FirstOrDefault(f => f.TechniqueId == t.Id)?.StartTimestamp?.ToString("mm\\:ss") ?? null,
                    EndTimestamp = feedbackRecords.FirstOrDefault(f => f.TechniqueId == t.Id)?.EndTimestamp?.ToString("mm\\:ss") ?? null,
                    }).ToList(),
                Drills = analysisResult.Drills.Select(d => new DrillDto
                {
                    Id = d.Id,
                    Name = d.Name,
                    Focus = d.Focus,
                    Duration = d.Duration.ToString(),
                    Description = d.Description,
                    RelatedTechniqueName = d.Technique.Name
                }).ToList(),
                Strengths = JsonSerializer.Deserialize<List<Strength>>(analysisResult.Strengths),
                AreasForImprovement = JsonSerializer.Deserialize<List<AreaForImprovement>>(analysisResult.AreasForImprovement),
                OverallDescription = analysisResult.OverallDescription
            };

            return analysisDto;
        }

        public async Task<AnalysisResultDto> SaveAnalysisResultDtoByVideoId(int videoId, AnalysisResultDto analysisDto)
        {
            var existingAiAnalysisResult = await _context.AiAnalysisResults
                .Include(a => a.Techniques)
                .Include(a => a.Drills)
                .FirstOrDefaultAsync(a => a.VideoId == videoId);

            if (existingAiAnalysisResult == null)
                throw new InvalidOperationException("No analysis result found.");

            // Update scalar properties if provided
            if (analysisDto.Strengths != null)
            {
                existingAiAnalysisResult.Strengths = JsonSerializer.Serialize(analysisDto.Strengths);
            }

            if (analysisDto.AreasForImprovement != null)
            {
                existingAiAnalysisResult.AreasForImprovement = JsonSerializer.Serialize(analysisDto.AreasForImprovement);
            }

            if (analysisDto.OverallDescription != null)
            {
                existingAiAnalysisResult.OverallDescription = analysisDto.OverallDescription;
            }

            // Update Techniques if provided
            if (analysisDto.Techniques != null)
            {
                existingAiAnalysisResult.Techniques ??= new List<Techniques>();

                // Remove techniques not in the request
                var techniquesToRemove = existingAiAnalysisResult.Techniques
                    .Where(t => !analysisDto.Techniques.Any(nt => nt.Id.HasValue && nt.Id == t.Id))
                    .ToList();
                foreach (var technique in techniquesToRemove)
                {
                    existingAiAnalysisResult.Techniques.Remove(technique);
                }

                // Update or add techniques
                foreach (var newTechnique in analysisDto.Techniques)
                {
                    var existingTechnique = existingAiAnalysisResult.Techniques
                        .FirstOrDefault(t => newTechnique.Id.HasValue && t.Id == newTechnique.Id);
                    if (existingTechnique != null)
                    {
                        existingTechnique.Name = newTechnique.Name ?? existingTechnique.Name;
                        if (newTechnique.TechniqueType != null)
                        {
                            var techniqueTypeName = newTechnique.TechniqueType.Name ?? "Unnamed Technique Type";
                            var positionalScenarioName = newTechnique.PositionalScenario?.Name ?? "Unnamed Positional Scenario";

                            // Check for existing PositionalScenario
                            var positionalScenario = await _context.PositionalScenarios
                                .FirstOrDefaultAsync(ps => ps.Name == positionalScenarioName);
                            if (positionalScenario == null)
                            {
                                positionalScenario = new PositionalScenario { Name = positionalScenarioName };
                                _context.PositionalScenarios.Add(positionalScenario);
                            }

                            // Check for existing TechniqueType
                            var techniqueType = await _context.TechniqueTypes
                                .FirstOrDefaultAsync(tt => tt.Name == techniqueTypeName && tt.PositionalScenario.Name == positionalScenarioName);
                            if (techniqueType == null)
                            {
                                techniqueType = new TechniqueType { Name = techniqueTypeName, PositionalScenario = positionalScenario };
                                _context.TechniqueTypes.Add(techniqueType);
                            }

                            existingTechnique.TechniqueType = techniqueType;
                        }
                    }
                    else
                    {
                        var techniqueTypeName = newTechnique.TechniqueType?.Name ?? "Unnamed Technique Type";
                        var positionalScenarioName = newTechnique.PositionalScenario?.Name ?? "Unnamed Positional Scenario";

                        var positionalScenario = new PositionalScenario { Name = positionalScenarioName };
                        var techniqueType = new TechniqueType { Name = techniqueTypeName, PositionalScenario = positionalScenario };

                        var techniqueToAdd = new Techniques
                        {
                            Name = newTechnique.Name ?? "Unnamed Technique",
                            Description = "Unnamed Technique",
                            TechniqueType = techniqueType
                        };
                        existingAiAnalysisResult.Techniques.Add(techniqueToAdd);
                    }
                }
            }

            // Update Drills if provided
            if (analysisDto.Drills != null)
            {
                existingAiAnalysisResult.Drills ??= new List<Drills>();
                existingAiAnalysisResult.Drills.Clear();
                foreach (var drillDto in analysisDto.Drills)
                {
                    if (string.IsNullOrEmpty(drillDto.RelatedTechniqueName))
                        throw new InvalidOperationException("Drill related technique name cannot be null or empty.");

                    // Find or create the related technique
                    existingAiAnalysisResult.Techniques ??= new List<Techniques>();
                    var technique = existingAiAnalysisResult.Techniques
                        .FirstOrDefault(t => t.Name == drillDto.RelatedTechniqueName);
                    if (technique == null)
                    {
                        technique = new Techniques { Name = drillDto.RelatedTechniqueName };
                        existingAiAnalysisResult.Techniques.Add(technique);
                    }

                    var drill = new Drills
                    {
                        Name = drillDto.Name ?? "Unnamed Drill",
                        Duration = ParseDuration(drillDto.Duration),
                        Focus = drillDto.Focus,
                        Description = drillDto.Description,
                        Technique = technique // EF will handle TechniqueId
                    };

                    existingAiAnalysisResult.Drills.Add(drill);
                }
            }

            // Mark the entity as modified and save changes
            _context.AiAnalysisResults.Update(existingAiAnalysisResult);
            await _context.SaveChangesAsync();

            return analysisDto;
        }

        private TimeSpan ParseDuration(string durationStr)
        {
            if (string.IsNullOrEmpty(durationStr))
                return TimeSpan.Zero;

            try
            {
                var parts = durationStr.ToLower().Split(' ');
                if (parts.Length > 0 && parts.Any(p => p.Contains("minute")))
                {
                    var numberPart = parts[0];
                    if (numberPart.Contains("-"))
                    {
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
            return TimeSpan.Zero;
        }
    }
}