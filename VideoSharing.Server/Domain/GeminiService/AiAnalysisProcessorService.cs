using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SharedEntities.Data;
using SharedEntities.Models;

namespace VideoSharing.Server.Domain.GeminiService
{
    public class AiAnalysisProcessorService
    {
        private readonly MyDatabaseContext _context;

        public AiAnalysisProcessorService(MyDatabaseContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<(List<Techniques> techniques, List<Drills> drills)> ProcessAnalysisJsonAsync(string json)
        {
            if (string.IsNullOrEmpty(json))
                throw new ArgumentException("JSON string cannot be null or empty.", nameof(json));

            // Deserialize JSON
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var analysis = JsonSerializer.Deserialize<AiAnalysisResultDto>(json, options)
                ?? throw new InvalidOperationException("Failed to deserialize JSON.");

            // Dictionary to map technique names to Technique entities
            var techniqueMap = new Dictionary<string, Techniques>();

            var techniquesProcessed = new List<Techniques>();
            var drillsProcessed = new List<Drills>();

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

                techniquesProcessed.Add(technique);
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
                    drillsProcessed.Add(drill);
                }
                // Missing related_technique can be logged or ignored based on requirements
            }

            await _context.SaveChangesAsync();
            return (techniquesProcessed, drillsProcessed);
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