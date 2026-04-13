using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Domain.IRepository;
using ElecWasteCollection.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElecWasteCollection.Infrastructure.Repository
{
    public class DashboardRepository : IDashboardRepository
    {
        private readonly ElecWasteCollectionDbContext _context;

        public DashboardRepository(ElecWasteCollectionDbContext context)
        {
            _context = context;
        }
        public async Task<int> CountUsersAsync(DateTime fromUtc, DateTime toUtc)
        {
            return await _context.Users
                .CountAsync(u => u.CreateAt >= fromUtc && u.CreateAt <= toUtc);
        }
        public async Task<int> CountCompaniesAsync(DateTime fromUtc, DateTime toUtc)
        {
            return await _context.Companies
                .CountAsync(c => c.Created_At >= fromUtc && c.Created_At <= toUtc);
        }
        public async Task<int> CountProductsAsync(DateOnly from, DateOnly to)
        {
            return await _context.Products
                .CountAsync(p => p.CreateAt >= from && p.CreateAt <= to);
        }
        public async Task<int> CountPackagesByScpIdAsync(string scpId, DateTime fromUtc, DateTime toUtc)
        {
            return await _context.Packages
                .Where(p => p.SmallCollectionPointsId == scpId)
                .CountAsync(p => p.CreateAt >= fromUtc && p.CreateAt <= toUtc);
        }
        public async Task<List<DateTime>> GetPackageCreationDatesByScpIdAsync(string scpId, DateTime fromUtc, DateTime toUtc)
        {
            return await _context.Packages
                .Where(p => p.SmallCollectionPointsId == scpId && p.CreateAt >= fromUtc && p.CreateAt <= toUtc)
                .Select(p => p.CreateAt)
                .ToListAsync();
        }
        public async Task<int> CountProductsByScpIdAsync(string scpId, DateOnly from, DateOnly to)
        {
            return await _context.Products
                .Where(p => p.SmallCollectionPointsId == scpId)
                .CountAsync(p => p.CreateAt >= from && p.CreateAt <= to);
        }
        public async Task<Dictionary<string, int>> GetProductCountsByCategoryByScpIdAsync(string scpId, DateOnly from, DateOnly to)
        {
            return await _context.Products
                .Where(p => p.SmallCollectionPointsId == scpId && p.CreateAt >= from && p.CreateAt <= to)
                .GroupBy(p => p.Category.Name)
                .Select(g => new { Name = g.Key, Count = g.Count() })
                .ToDictionaryAsync(k => k.Name, v => v.Count);
        }
        public async Task<Dictionary<string, int>> GetProductCountsByBrandByScpIdAsync(string scpId, DateOnly from, DateOnly to)
        {
            return await _context.Products
                .Where(p => p.SmallCollectionPointsId == scpId && p.CreateAt >= from && p.CreateAt <= to)
                .GroupBy(p => p.Brand.Name)
                .Select(g => new {
                    BrandName = g.Key ?? "N/A",
                    Count = g.Count()
                })
                .ToDictionaryAsync(k => k.BrandName, v => v.Count);
        }
        public async Task<List<(Guid UserId, string Name, string Email, int Count)>> GetTopContributingUsersRawAsync(string scpId, int top, DateOnly from, DateOnly to)
        {
            var cleanId = scpId.Trim();

            var data = await _context.Products
                .Where(p => p.SmallCollectionPointsId == cleanId && p.CreateAt >= from && p.CreateAt <= to)
                .GroupBy(p => new { p.UserId, p.User.Name, p.User.Email })
                .Select(g => new
                {
                    g.Key.UserId,
                    Name = g.Key.Name ?? "N/A",
                    Email = g.Key.Email ?? "N/A",
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .Take(top)
                .ToListAsync();

            return data.Select(x => (x.UserId, x.Name, x.Email, x.Count)).ToList();
        }

        public async Task<List<(Guid ProductId, string CategoryName, string BrandName, string Status, DateOnly? CreateAt)>> GetProductsByUserIdRawAsync(Guid userId)
        {
            // Đảm bảo dùng .AsNoTracking() để tăng tốc và kiểm tra kỹ UserId
            var data = await _context.Products
                .AsNoTracking()
                .Where(p => p.UserId == userId)
                .Select(p => new
                {
                    p.ProductId,
                    CategoryName = p.Category.Name, // Đảm bảo Navigation Property Category không null
                    BrandName = p.Brand.Name,       // Đảm bảo Navigation Property Brand không null
                    p.Status,
                    p.CreateAt
                })
                .OrderByDescending(p => p.CreateAt)
                .ToListAsync();

            return data.Select(x => (x.ProductId, x.CategoryName, x.BrandName, x.Status, x.CreateAt)).ToList();
        }
    }
}