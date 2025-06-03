using Microsoft.EntityFrameworkCore;
using SharedEntities.Data;
using SharedEntities.Models;

namespace FighterManager.Server.Repository
{
    public interface IAttendanceRepository
    {
        Task<TrainingSession?> GetSessionWithDetailsAsync(int sessionId);
        Task<AppUserEntity?> GetAppUserByFighterIdAsync(int fighterId);
        Task<Fighter?> GetFighterByNameAsync(string fighterName);
        Task<Fighter> AddFighterAsync(Fighter fighter);
        Task<Fighter> UpdateFighterAsync(Fighter fighter);
        Task AddSessionFighterJointAsync(TrainingSessionFighterJoint joint);
        void RemoveSessionFighterJointAsync(TrainingSessionFighterJoint joint);
        Task<TrainingSessionFighterJoint?> GetSessionFighterJointAsync(int sessionId, int fighterId);
        Task SaveChangesAsync();
    }

    public class AttendanceRepository : IAttendanceRepository
    {
        private readonly MyDatabaseContext _context;

        public AttendanceRepository(MyDatabaseContext context)
        {
            _context = context;
        }

        public async Task<TrainingSession?> GetSessionWithDetailsAsync(int sessionId)
        {
            return await _context.TrainingSessions
                .Include(s => s.Students!)
                .ThenInclude(j => j.Fighter!)
                .Include(s => s.Instructor!)
                .FirstOrDefaultAsync(s => s.Id == sessionId);
        }

        public async Task<AppUserEntity?> GetAppUserByFighterIdAsync(int fighterId)
        {
            return await _context.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.FighterId == fighterId);
        }

        public async Task<Fighter?> GetFighterByNameAsync(string fighterName)
        {   // Add for read-only queries
            return await _context.Fighters
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.FighterName.ToLower() == fighterName.ToLower());
        }

        public async Task<Fighter> AddFighterAsync(Fighter fighter)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.Fighters.AddAsync(fighter);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return fighter;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<Fighter> UpdateFighterAsync(Fighter fighter)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.Fighters.Update(fighter);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return fighter;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task AddSessionFighterJointAsync(TrainingSessionFighterJoint joint)
        {
            await _context.TrainingSessionFighterJoints.AddAsync(joint);
        }

        public void RemoveSessionFighterJointAsync(TrainingSessionFighterJoint joint)
        {
            _context.TrainingSessionFighterJoints.Remove(joint);
        }

        public async Task<TrainingSessionFighterJoint?> GetSessionFighterJointAsync(int sessionId, int fighterId)
        {
            return await _context.TrainingSessionFighterJoints
                .FirstOrDefaultAsync(j => j.TrainingSessionId == sessionId && j.FighterId == fighterId);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}