using SharedEntities.Data;
using MatchMaker.Server.Domain.PairMatchingService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MatchMaker.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class MatchMakerController(MyDatabaseContext databaseContext) : ControllerBase
    {
        private readonly MyDatabaseContext _databaseContext = databaseContext;

        [HttpPost]
        public async Task<IActionResult> GeneratePairs([FromBody] MatchMakerDto request)
        {
            // Fetch all Students records from the database based on the provided StudentFighterIds
            var studentFighters = await _databaseContext.Fighters
                .Where(f => request.StudentFighterIds.Contains(f.Id))
                .ToListAsync();

            if (!studentFighters.Any())
            {
                return BadRequest("No valid students found for the given IDs.");
            }

            // Fetch the Instructor record from the database
            var instructor = await _databaseContext.Fighters
                .FirstOrDefaultAsync(f => f.Id == request.InstructorFighterId);

            if (instructor == null)
            {
                return BadRequest("No valid instructor found for the given ID.");
            }

            // Instantiate the PairMatchingService based on whether HowManyUniquePairs is provided
            PairMatchingService pairMatchingService;
            if (request.HowManyUniquePairs.HasValue)
            {
                pairMatchingService = new PairMatchingService(studentFighters, instructor, request.HowManyUniquePairs.Value);
            }
            else
            {
                pairMatchingService = new PairMatchingService(studentFighters, instructor);
            }

            var generatedPairs = pairMatchingService.GenerateNonUniquePairs();

            var response = generatedPairs.Select(pair => new
            {
                Fighter1 = pair.Item1.FighterName,
                Fighter2 = pair.Item2.FighterName
            }).ToList();

            return Ok(response);
        }
    }

    public class MatchMakerDto
    {
        public required List<int> StudentFighterIds { get; set; }

        public required int InstructorFighterId { get; set; }

        public int? HowManyUniquePairs { get; set; }
    }
}
