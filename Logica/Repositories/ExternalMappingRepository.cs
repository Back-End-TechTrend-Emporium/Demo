using Data;
using Data.Entities;
using Data.Entities.Enums;
using Logica.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Logica.Repositories
{
    public class ExternalMappingRepository : IExternalMappingRepository
    {
        private readonly AppDbContext _context;

        public ExternalMappingRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ExternalMapping?> GetMappingAsync(string sourceId, ExternalSource source, string sourceType)
        {
            return await _context.ExternalMappings
                .FirstOrDefaultAsync(em => em.SourceId == sourceId && 
                                          em.Source == source && 
                                          em.SourceType == sourceType);
        }

        public async Task<IEnumerable<ExternalMapping>> GetMappingsBySourceIdsAsync(IEnumerable<string> sourceIds, ExternalSource source, string sourceType)
        {
            return await _context.ExternalMappings
                .Where(em => sourceIds.Contains(em.SourceId) && 
                            em.Source == source && 
                            em.SourceType == sourceType)
                .ToListAsync();
        }

        public async Task<Dictionary<string, Guid>> GetInternalIdMappingsAsync(IEnumerable<string> sourceIds, ExternalSource source, string sourceType)
        {
            var mappings = await GetMappingsBySourceIdsAsync(sourceIds, source, sourceType);
            return mappings.ToDictionary(m => m.SourceId, m => m.InternalId);
        }

        public async Task<ExternalMapping?> GetByExternalIdAsync(ExternalSource source, string sourceType, string sourceId)
        {
            return await _context.ExternalMappings
                .FirstOrDefaultAsync(em => em.Source == source && 
                                          em.SourceType == sourceType && 
                                          em.SourceId == sourceId);
        }

        public async Task<ExternalMapping> CreateAsync(ExternalMapping mapping)
        {
            _context.ExternalMappings.Add(mapping);
            await _context.SaveChangesAsync();
            return mapping;
        }
    }
}