using CineReserv.Models;

namespace CineReserv.Services
{
    public interface IApiService
    {
        Task<List<Film>> GetFilmsFromApiAsync();
        Task<Film> GetFilmByIdAsync(int id);
        Task SeedDatabaseAsync();
        Task ForceSeedDatabaseAsync();
    }
}
