using Microsoft.EntityFrameworkCore;
using FighterManager.Server.Data;
using FighterManager.Server.Helpers;
using FighterManager.Server.Models;
using System.Net;

namespace FighterManager.Server.Repository
{
    public interface IFighterRepository
    {
        Task<List<Fighter>> GetFighters();

        Task<Fighter?> GetFighter(int id);

        Task<Fighter> AddFighter(Fighter fighter);

        Task<Fighter> UpdateFighter(Fighter fighter);

        Task<Fighter> DeleteFighter(int id);
    }

    public class FighterRepository(IServiceProvider serviceProvider) : IFighterRepository
    {
        private readonly MyDatabaseContext _databaseContext =
            serviceProvider.CreateScope().ServiceProvider.GetRequiredService<MyDatabaseContext>();

        public async Task<List<Fighter>> GetFighters()
        {
            return await _databaseContext.Fighters.ToListAsync();
        }

        public async Task<Fighter?> GetFighter(int id)
        {
            var existingFighter = await _databaseContext.Fighters.FindAsync(id);

            if (existingFighter == null)
            {
                return null;
            }

            return existingFighter;
        }

        public async Task<Fighter> AddFighter(Fighter fighter)
        {
            try
            {
                _databaseContext.Fighters.Add(fighter);
                await _databaseContext.SaveChangesAsync();
                return fighter;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public async Task<Fighter> UpdateFighter(Fighter fighter)
        {
            try
            {
                _databaseContext.Entry(fighter).State = EntityState.Modified;
                await _databaseContext.SaveChangesAsync();
                return fighter;
            }
            catch (DbUpdateException e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public async Task<Fighter> DeleteFighter(int id)
        {
            var fighter = await _databaseContext.Fighters.FindAsync(id);
            if (fighter == null)
            {
                throw new ErrorResponseException().SetMessage("Fighter Not Found!").SetStatusCode(HttpStatusCode.BadRequest);
            }

            _databaseContext.Fighters.Remove(fighter);
            await _databaseContext.SaveChangesAsync();
            return fighter;
        }
    }
}
