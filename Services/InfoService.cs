using Microsoft.EntityFrameworkCore;
using Note_taking_demo.Data;
using Note_taking_demo.Models;
using Microsoft.ServiceFabric.Actors.Query;

namespace Note_taking_demo.Services;

public class InfoService
{
    private readonly AppDbContext _db;

    public InfoService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<Info>> GetForUserAsync(int userId, int page = 1, int pageSize = 4)
    {
        if (page < 1)
            page = 1;

        var skip = (page - 1) * pageSize;

        return await _db.Infos
            .Where(i => i.UserId == userId)
            .OrderByDescending(i => i.Id) // tai CreatedAt
            .Skip(skip)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync();
    }


    public async Task AddAsync(int userId, string content)
    {
        var info = new Info
        {
            UserId = userId,
            Note = content
        };

        _db.Infos.Add(info);
        await _db.SaveChangesAsync();
    }
    
    public async Task<bool> DeleteAsync(int infoId, int userId)
    {
        var info = await _db.Infos
            .Where(i => i.UserId == userId)
            .OrderByDescending(i => i.Id == infoId)
            .FirstOrDefaultAsync();
        
        if (info != null)
        {
            _db.Infos.Remove(info);
            await _db.SaveChangesAsync();
            return true;
        } else {
            return false;
        }
    }
    
    public async Task<bool> UpdateAsync(int infoId, string content, int userId)
    {
        var info = await _db.Infos
            .Where(i => i.UserId == userId)
            .OrderByDescending(i => i.Id == infoId)
            .FirstOrDefaultAsync();
        
        if (info != null)
        {
            info.Note = content;
            await _db.SaveChangesAsync();
            return true;
        } else {
            return false;
        }
    }
}
