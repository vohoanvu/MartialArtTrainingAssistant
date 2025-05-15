using System.Globalization;
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
            var existingAiAnalysisResult = await _context.AiAnalysisResults.FirstOrDefaultAsync(a => a.VideoId == videoId);
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
                    Drills = new List<Drills>(),
                    GeneratedAt = DateTime.UtcNow,
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
                existingAiAnalysisResult.LastUpdatedAt = DateTime.UtcNow;
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
                    .Include(t => t.TechniqueType) // Ensure TechniqueType is loaded
                    .FirstOrDefaultAsync(t => t.Name == tech.TechniqueName && t.TechniqueType != null && t.TechniqueType.Name == tech.TechniqueType);
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

                // Explicitly link the technique to the current AiAnalysisResult
                technique.AiAnalysisResult = existingAiAnalysisResult;
                if (!existingAiAnalysisResult.Techniques.Any(t => t.Name == tech.TechniqueName 
                    && t.TechniqueType != null && t.TechniqueType.Name == tech.TechniqueType))
                {
                    existingAiAnalysisResult.Techniques.Add(technique);
                }

                // Get or create AiFeedback
                var aiFeedback = await _context.VideoSegmentFeedbacks.FirstOrDefaultAsync(f => f.VideoId == videoId && f.TechniqueId == technique.Id);
                if (aiFeedback == null)
                {
                    aiFeedback = new VideoSegmentFeedback
                    {
                        VideoId = videoId,
                        Technique = technique,
                        StartTimestamp = tech.StartTimestamp != null && TimeSpan.TryParseExact(tech.StartTimestamp, "hh\\:mm\\:ss", CultureInfo.InvariantCulture, out var startResult) ? startResult : null,
                        EndTimestamp = tech.EndTimestamp != null && TimeSpan.TryParseExact(tech.EndTimestamp, "hh\\:mm\\:ss", CultureInfo.InvariantCulture, out var endResult) ? endResult : null,
                    };
                    _context.VideoSegmentFeedbacks.Add(aiFeedback);
                }
                else 
                {
                    aiFeedback.StartTimestamp = tech.StartTimestamp != null && TimeSpan.TryParseExact(tech.StartTimestamp, "hh\\:mm\\:ss", CultureInfo.InvariantCulture, out var startResult) ? startResult : null;
                    aiFeedback.EndTimestamp = tech.EndTimestamp != null && TimeSpan.TryParseExact(tech.EndTimestamp, "hh\\:mm\\:ss", CultureInfo.InvariantCulture, out var endResult) ? endResult : null;
                    aiFeedback.Technique = technique;
                }

                techniqueMap[tech.TechniqueName] = technique;
            }

            // Process suggested_drills
            foreach (var drillDto in analysis.SuggestedDrills)
            {
                if (!string.IsNullOrWhiteSpace(drillDto.RelatedTechnique) && techniqueMap.TryGetValue(drillDto.RelatedTechnique, out var technique))
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
                            Duration = drillDto.Duration,
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
                        drill.Duration = drillDto.Duration;
                        if (!existingAiAnalysisResult.Drills.Any(d => d.Id == drill.Id))
                        {
                            existingAiAnalysisResult.Drills.Add(drill);
                        }
                    }
                }
                else
                {
                    //Just in case the AI model forgot to specify the related Technique, associate that drill with the default generic technique
                    var genericTechnique = await _context.Techniques.FirstAsync(t => t.Name == "Generic");
                    var drill = new Drills
                    {
                        Name = drillDto.Name,
                        Description = drillDto.Description,
                        Focus = drillDto.Focus,
                        Duration = drillDto.Duration,
                        TechniqueId = genericTechnique.Id
                    };
                    _context.Drills.Add(drill);
                    existingAiAnalysisResult.Drills.Add(drill);
                }
            }

            // Process areas_for_improvement for weaknesses
            foreach (var area in analysis.AreasForImprovement)
            {
                if (!string.IsNullOrEmpty(area.WeaknessCategory))
                {
                    var category = await _context.WeaknessCategories.FirstOrDefaultAsync(w => w.Name == area.WeaknessCategory);
                    if (category == null)
                    {
                        category = new WeaknessCategory
                        {
                            Name = area.WeaknessCategory,
                            Description = area.Description // Optional: Use description for context
                        };
                        _context.WeaknessCategories.Add(category);
                        await _context.SaveChangesAsync(); // Ensure category has an ID
                    }

                    var analysisWeakness = new AnalysisWeakness
                    {
                        AiAnalysisResultId = existingAiAnalysisResult.Id,
                        WeaknessCategoryId = category.Id
                    };
                    _context.AnalysisWeaknesses.Add(analysisWeakness);
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
            var feedbackRecords = await _context.VideoSegmentFeedbacks
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
                        PositionalScenarioId = t.TechniqueType.PositionalScenario.Id,
                    },
                    PositionalScenario = new PositionalScenarioDto
                    {
                        Id = t.TechniqueType.PositionalScenario.Id,
                        Name = t.TechniqueType.PositionalScenario.Name
                    },
                    StartTimestamp = feedbackRecords.FirstOrDefault(f => f.TechniqueId == t.Id)?.StartTimestamp?.ToString("hh\\:mm\\:ss", CultureInfo.InvariantCulture) ?? null,
                    EndTimestamp = feedbackRecords.FirstOrDefault(f => f.TechniqueId == t.Id)?.EndTimestamp?.ToString("hh\\:mm\\:ss", CultureInfo.InvariantCulture) ?? null,
                    }).ToList(),
                Drills = analysisResult.Drills.Select(d => new DrillDto
                {
                    Id = d.Id,
                    Name = d.Name,
                    Focus = d.Focus,
                    Duration = d.Duration,
                    Description = d.Description,
                    RelatedTechniqueName = d.Technique.Name,
                    RelatedTechniqueId = d.Technique.Id
                }).ToList(),
                Strengths = JsonSerializer.Deserialize<List<StrengthDto>>(analysisResult.Strengths),
                AreasForImprovement = JsonSerializer.Deserialize<List<AreasForImprovementDto>>(analysisResult.AreasForImprovement),
                OverallDescription = analysisResult.OverallDescription
            };

            if (analysisDto.Strengths != null) 
            {
                foreach (var strenthDto in analysisDto.Strengths)
                {
                    if (strenthDto.RelatedTechnique != null) {
                        strenthDto.RelatedTechniqueId = analysisDto.Techniques.FirstOrDefault(t => t.Name == strenthDto.RelatedTechnique)?.Id;
                    }
                }
            }

            if (analysisDto.AreasForImprovement != null) 
            {
                foreach (var areasForImprovementDto in analysisDto.AreasForImprovement)
                {
                    if (areasForImprovementDto.RelatedTechnique != null) {
                        areasForImprovementDto.RelatedTechniqueId = analysisDto.Techniques.FirstOrDefault(t => t.Name == areasForImprovementDto.RelatedTechnique)?.Id;
                    }
                }
            }

            return analysisDto;
        }

        public async Task<AnalysisResultDto> SavePartialAnalysisResultDtoByVideoId(int videoId, PartialAnalysisResultDto partialDto)
        {
            var existingAiAnalysisResult = await _context.AiAnalysisResults
                .Include(a => a.Techniques).ThenInclude(t => t.TechniqueType).ThenInclude(tt => tt.PositionalScenario)
                .Include(a => a.Drills).ThenInclude(d => d.Technique)
                .FirstOrDefaultAsync(a => a.VideoId == videoId);

            if (existingAiAnalysisResult == null)
                throw new InvalidOperationException("No analysis result found.");

            // Update scalar properties if provided
            if (partialDto.Strengths != null)
            {
                existingAiAnalysisResult.Strengths = JsonSerializer.Serialize(partialDto.Strengths);
            }

            if (partialDto.AreasForImprovement != null)
            {
                existingAiAnalysisResult.AreasForImprovement = JsonSerializer.Serialize(partialDto.AreasForImprovement);
            }

            if (partialDto.OverallDescription != null)
            {
                existingAiAnalysisResult.OverallDescription = partialDto.OverallDescription;
            }

            // Update Techniques if provided
            if (partialDto.Techniques != null)
            {
                existingAiAnalysisResult.Techniques ??= new List<Techniques>();
                var existingTechniques = existingAiAnalysisResult.Techniques.ToList();

                // Update or add techniques
                foreach (var newTechnique in partialDto.Techniques)
                {
                    var existingTechnique = existingTechniques.FirstOrDefault(t => newTechnique.Id.HasValue && t.Id == newTechnique.Id);
                    if (existingTechnique != null)
                    {
                        // Update existing technique
                        existingTechnique.Name = newTechnique.Name ?? existingTechnique.Name;
                        existingTechnique.Description = newTechnique.Description ?? existingTechnique.Description;
                        if (newTechnique.TechniqueType != null)
                        {
                            var techniqueTypeName = newTechnique.TechniqueType.Name ?? existingTechnique.TechniqueType.Name;
                            var positionalScenarioName = newTechnique.PositionalScenario?.Name ?? existingTechnique.TechniqueType.PositionalScenario.Name;

                            var positionalScenario = await _context.PositionalScenarios
                                .FirstOrDefaultAsync(ps => ps.Name == positionalScenarioName);
                            if (positionalScenario == null)
                            {
                                positionalScenario = new PositionalScenario { Name = positionalScenarioName };
                                _context.PositionalScenarios.Add(positionalScenario);
                            }

                            var techniqueType = await _context.TechniqueTypes
                                .FirstOrDefaultAsync(tt => tt.Name == techniqueTypeName && tt.PositionalScenario.Name == positionalScenarioName);
                            if (techniqueType == null)
                            {
                                techniqueType = new TechniqueType { Name = techniqueTypeName, PositionalScenario = positionalScenario };
                                _context.TechniqueTypes.Add(techniqueType);
                            }

                            existingTechnique.TechniqueType = techniqueType;
                        }

                        // Update AiFeedback for timestamps
                        var aiFeedback = await _context.VideoSegmentFeedbacks
                            .FirstOrDefaultAsync(f => f.VideoId == videoId && f.TechniqueId == existingTechnique.Id);
                        if (aiFeedback == null)
                        {
                            aiFeedback = new VideoSegmentFeedback
                            {
                                VideoId = videoId,
                                Technique = existingTechnique,
                            };
                            _context.VideoSegmentFeedbacks.Add(aiFeedback);
                        }
                        aiFeedback.StartTimestamp = newTechnique.StartTimestamp != null && TimeSpan.TryParseExact(newTechnique.StartTimestamp, "hh\\:mm\\:ss", CultureInfo.InvariantCulture, out var startResult)
                            ? startResult
                            : aiFeedback.StartTimestamp;
                        aiFeedback.EndTimestamp = newTechnique.EndTimestamp != null && TimeSpan.TryParseExact(newTechnique.EndTimestamp, "hh\\:mm\\:ss", CultureInfo.InvariantCulture, out var endResult)
                            ? endResult
                            : aiFeedback.EndTimestamp;
                    }
                    else
                    {
                        // Add new technique
                        var techniqueTypeName = newTechnique.TechniqueType?.Name ?? "Unnamed Technique Type";
                        var positionalScenarioName = newTechnique.PositionalScenario?.Name ?? "Unnamed Positional Scenario";

                        var positionalScenario = await _context.PositionalScenarios
                            .FirstOrDefaultAsync(ps => ps.Name == positionalScenarioName);
                        if (positionalScenario == null)
                        {
                            positionalScenario = new PositionalScenario { Name = positionalScenarioName };
                            _context.PositionalScenarios.Add(positionalScenario);
                        }

                        var techniqueType = await _context.TechniqueTypes
                            .FirstOrDefaultAsync(tt => tt.Name == techniqueTypeName && tt.PositionalScenario.Name == positionalScenarioName);
                        if (techniqueType == null)
                        {
                            techniqueType = new TechniqueType { Name = techniqueTypeName, PositionalScenario = positionalScenario };
                            _context.TechniqueTypes.Add(techniqueType);
                        }

                        var techniqueToAdd = new Techniques
                        {
                            Name = newTechnique.Name ?? "Unnamed Technique",
                            Description = newTechnique.Description ?? "No description",
                            TechniqueType = techniqueType
                        };
                        existingAiAnalysisResult.Techniques.Add(techniqueToAdd);

                        // Add AiFeedback for new technique
                        var aiFeedback = new VideoSegmentFeedback
                        {
                            VideoId = videoId,
                            Technique = techniqueToAdd,
                            StartTimestamp = newTechnique.StartTimestamp != null && TimeSpan.TryParseExact(newTechnique.StartTimestamp, "hh\\:mm\\:ss", CultureInfo.InvariantCulture, out var startResult)
                                ? startResult
                                : null,
                            EndTimestamp = newTechnique.EndTimestamp != null && TimeSpan.TryParseExact(newTechnique.EndTimestamp, "hh\\:mm\\:ss", CultureInfo.InvariantCulture, out var endResult)
                                ? endResult
                                : null,
                        };
                        _context.VideoSegmentFeedbacks.Add(aiFeedback);
                    }
                }

                // Remove techniques marked for deletion (not in partialDto.Techniques)
                var techniquesToRemove = existingTechniques
                    .Where(t => !partialDto.Techniques.Any(nt => nt.Id.HasValue && nt.Id == t.Id))
                    .ToList();
                foreach (var technique in techniquesToRemove)
                {
                    existingAiAnalysisResult.Techniques.Remove(technique);
                    var aiFeedback = await _context.VideoSegmentFeedbacks
                        .FirstOrDefaultAsync(f => f.VideoId == videoId && f.TechniqueId == technique.Id);
                    if (aiFeedback != null)
                    {
                        _context.VideoSegmentFeedbacks.Remove(aiFeedback);
                    }
                }
            }

            // Update Drills if provided
            if (partialDto.Drills != null)
            {
                existingAiAnalysisResult.Drills ??= new List<Drills>();
                var existingDrills = existingAiAnalysisResult.Drills.ToList();

                // Update or add drills
                foreach (var newDrill in partialDto.Drills)
                {
                    var existingDrill = existingDrills.FirstOrDefault(d => newDrill.Id.HasValue && d.Id == newDrill.Id);
                    if (existingDrill != null)
                    {
                        // Update existing drill
                        existingDrill.Name = newDrill.Name ?? existingDrill.Name;
                        existingDrill.Description = newDrill.Description ?? existingDrill.Description;
                        existingDrill.Focus = newDrill.Focus ?? existingDrill.Focus;
                        existingDrill.Duration = newDrill.Duration ?? existingDrill.Duration;
                        if (!string.IsNullOrEmpty(newDrill.RelatedTechniqueName))
                        {
                            var technique = existingAiAnalysisResult.Techniques?.FirstOrDefault(t => t.Name == newDrill.RelatedTechniqueName)
                                ?? await _context.Techniques.FirstOrDefaultAsync(t => t.Name == newDrill.RelatedTechniqueName)
                                ?? new Techniques { Name = newDrill.RelatedTechniqueName };
                            if (!existingAiAnalysisResult.Techniques.Contains(technique))
                            {
                                existingAiAnalysisResult.Techniques.Add(technique);
                            }
                            existingDrill.Technique = technique;
                        }
                    }
                    else
                    {
                        // Add new drill
                        var technique = !string.IsNullOrEmpty(newDrill.RelatedTechniqueName)
                            ? (existingAiAnalysisResult.Techniques?.FirstOrDefault(t => t.Name == newDrill.RelatedTechniqueName)
                                ?? await _context.Techniques.FirstOrDefaultAsync(t => t.Name == newDrill.RelatedTechniqueName)
                                ?? new Techniques { Name = newDrill.RelatedTechniqueName })
                            : await _context.Techniques.FirstOrDefaultAsync(t => t.Name == "Generic")
                              ?? new Techniques { Name = "Generic" };

                        if (!existingAiAnalysisResult.Techniques.Contains(technique))
                        {
                            existingAiAnalysisResult.Techniques.Add(technique);
                        }

                        var drillToAdd = new Drills
                        {
                            Name = newDrill.Name ?? "Unnamed Drill",
                            Description = newDrill.Description ?? "No description",
                            Focus = newDrill.Focus,
                            Duration = newDrill.Duration ?? "Unknown",
                            Technique = technique
                        };
                        existingAiAnalysisResult.Drills.Add(drillToAdd);
                    }
                }

                // Remove drills marked for deletion (not in partialDto.Drills)
                var drillsToRemove = existingDrills
                    .Where(d => !partialDto.Drills.Any(nd => nd.Id.HasValue && nd.Id == d.Id))
                    .ToList();
                foreach (var drill in drillsToRemove)
                {
                    existingAiAnalysisResult.Drills.Remove(drill);
                }
            }

            var video = await _context.Videos.FindAsync(videoId);
            existingAiAnalysisResult.LastUpdatedAt = DateTime.UtcNow;
            existingAiAnalysisResult.UpdatedBy = video?.UserId;
            _context.AiAnalysisResults.Update(existingAiAnalysisResult);
            await _context.SaveChangesAsync();

            // Return the updated AnalysisResultDto
            return await GetAnalysisResultDtoByVideoId(videoId);
        }
    }
}