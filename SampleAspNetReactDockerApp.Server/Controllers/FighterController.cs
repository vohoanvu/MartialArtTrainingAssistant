using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SampleAspNetReactDockerApp.Server.Domain.FighterService;
using SampleAspNetReactDockerApp.Server.Helpers;
using SampleAspNetReactDockerApp.Server.Models;
using SampleAspNetReactDockerApp.Server.Models.Dtos;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;

namespace SampleAspNetReactDockerApp.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FighterController(IUnitOfWork unitOfWork,
        IMapper objectMapper, 
        FighterRegistrationService fighterRegistrationService,
        UserManager<AppUserEntity> userManager, 
        FighterSignInService<AppUserEntity> fighterSignInService) : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly IMapper _objectMapper = objectMapper;
        private readonly FighterRegistrationService _fighterRegistrationService = fighterRegistrationService;
        private readonly UserManager<AppUserEntity> _userManager = userManager;
        private readonly FighterSignInService<AppUserEntity> _fighterSignInService = fighterSignInService;

        [HttpGet]
        [Authorize]
        public async Task<ActionResult<List<ViewFighterDto>>> GetListAsync()
        {
            var allFighters = await _unitOfWork.Repository<Fighter>().GetAllAsync();
            return Ok(_objectMapper.Map<List<Fighter>, List<ViewFighterDto>>(allFighters.ToList()));
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<ViewFighterDto>> GetByIdAsync(int id)
        {
            var fighter = await _unitOfWork.Repository<Fighter>().GetByIdAsync(id);
            if (fighter == null)
                return NotFound();

            return Ok(_objectMapper.Map<Fighter, ViewFighterDto>(fighter));
        }

        [HttpPost("register")]
        public async Task<ActionResult<ViewFighterDto>> CreateAsync(CreateFighterDto input)
        {
            if (!Enum.IsDefined(typeof(Gender), input.Gender) || 
                !Enum.IsDefined(typeof(FighterRole), input.FighterRole) ||
                !Enum.IsDefined(typeof(BeltColor), input.BeltColor) ||
                !Enum.IsDefined(typeof(TrainingExperience), input.Experience))
            {
                return BadRequest(new CustomRegistrationResponse
                {
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Title = "One or more validation errors occurred.",
                    Status = 400,
                    Detail = "Invalid Enum values",
                    Instance = HttpContext.TraceIdentifier,
                    Errors = new Dictionary<string, List<string>>
                    {
                        { "Gender", new List<string> { "Invalid gender value" } },
                        { "FighterRole", new List<string> { "Invalid fighter role value" } },
                        { "BeltColor", new List<string> { "Invalid belt color value" } },
                        { "Experience", new List<string> { "Invalid experience value" } }
                    }
                });
            }

            var result = await _fighterRegistrationService.RegisterFighterAsync(input);
            if (!result.identityResult.Succeeded)
            {
                return BadRequest(new CustomRegistrationResponse
                {
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Title = "Registration failed",
                    Status = 400,
                    Detail = "User registration failed",
                    Instance = HttpContext.TraceIdentifier,
                    Errors = result.identityResult.Errors.ToDictionary(
                        e => e.Code, 
                        e => new List<string> { e.Description }
                    )
                });
            }

            Console.WriteLine($"Created fighter with ID: {result.fighter!.Id}");

            return StatusCode(201, new CustomRegistrationResponse
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.3.2",
                Title = "Fighter created successfully",
                Status = 201,
                Detail = "The fighter was created successfully",
                Instance = HttpContext.TraceIdentifier,
                CreatedObject = result.fighter!
            });
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<ActionResult<ViewFighterDto>> UpdateAsync(int id, UpdateFighterDto input)
        {
            if (!Enum.IsDefined(typeof(Gender), input.Gender) ||
                !Enum.IsDefined(typeof(FighterRole), input.FighterRole) ||
                !Enum.IsDefined(typeof(BeltColor), input.BeltColor))
            {
                return BadRequest(new { Message = "Invalid Enum values" });
            }

            var existingFighter = await _unitOfWork.Repository<Fighter>().GetByIdAsync(id);
            if (existingFighter == null)
            {
                return NotFound(new { Message = "Fighter Not Found!" });
            }

            existingFighter.Update(input);
            _unitOfWork.Repository<Fighter>().Update(existingFighter);
            await _unitOfWork.SaveChangesAsync();
            return Ok(_objectMapper.Map<Fighter, ViewFighterDto>(existingFighter));
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<ActionResult> DeleteAsync(int id)
        {
            try
            {
                var fighter = await _unitOfWork.Repository<Fighter>().GetByIdAsync(id);
                if (fighter == null)
                    return NotFound();

                _unitOfWork.Repository<Fighter>().Delete(fighter);
                await _unitOfWork.SaveChangesAsync();
                return NoContent();
            }
            catch (ErrorResponseException ex)
            {
                return StatusCode((int)ex.StatusCode, new { ex.Message });
            }
        }

        [HttpGet("/api/fighter/info")]
        [Authorize]
        public async Task<IActionResult> GetUserInfo()
        {
            var user = await _userManager.Users.Include(u => u.Fighter)
                .FirstOrDefaultAsync(u => u.Id == _userManager.GetUserId(User));

            if (user == null || user.Fighter == null)
            {
                return NotFound(new { Message = "Fighter/User not found" });
            }

            var userInfo = new
            {
                user.Email,
                IsEmailConfirmed = user.EmailConfirmed,
                Fighter = new
                {
                    user.Fighter!.Id,
                    user.Fighter.FighterName,
                    user.Fighter.BelkRank,
                    user.Fighter.Role,
                    user.Fighter.Birthdate
                }
            };

            return Ok(userInfo);
        }


        [HttpPost("/api/fighter/login")]
        public async Task<IActionResult> Login([FromBody] CustomLoginRequest model)
        {
            if (model == null || string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.Password))
            {
                return BadRequest(new { message = "Email and password are required" });
            }

            try
            {
                var signInResult = await _fighterSignInService.PasswordSignInAsync(model.Email, model.Password, true, false);

                if (!signInResult.Succeeded)
                {
                    return Unauthorized(new { message = "Invalid login attempt" });
                }


                var user = await _userManager.FindByNameAsync(model.Email);
                if (user == null) 
                {
                    throw new ErrorResponseException()
                            .SetStatusCode(HttpStatusCode.InternalServerError)
                            .SetMessage("The Identity SignIn operation failed.");
                }

                var token = await _fighterSignInService.GenerateJwtTokenAsync(user);
                var refreshToken = _fighterSignInService.GenerateRefreshToken(user);

                var loginResponse = new CustomLoginResponse
                {
                    TokenType = "Bearer",
                    AccessToken = token,
                    ExpiresIn = 3600,
                    RefreshToken = refreshToken
                };

                return Ok(loginResponse);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, new { message = "An unexpected error occurred.", detail = ex.Message });
            }
        }
    }

}
