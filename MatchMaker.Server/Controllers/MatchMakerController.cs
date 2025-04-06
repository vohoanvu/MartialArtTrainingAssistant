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
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public class MatchMakerController(MyDatabaseContext databaseContext) : ControllerBase
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    {
        private readonly MyDatabaseContext _databaseContext = databaseContext;

        [HttpPost]
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public async Task<IActionResult> GeneratePairs([FromBody] MatchMakerDto request)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
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

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public class MatchMakerDto
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public required List<int> StudentFighterIds { get; set; }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public required int InstructorFighterId { get; set; }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public int? HowManyUniquePairs { get; set; }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    }
}
