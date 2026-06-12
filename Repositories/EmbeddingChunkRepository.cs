using Microsoft.EntityFrameworkCore;
using NutriFlow.Data;
using NutriFlow.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NutriFlow.Repositories
{
    public class EmbeddingChunkRepository : Repository<EmbeddingChunk>, IEmbeddingChunkRepository
    {
        public EmbeddingChunkRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<List<EmbeddingChunk>> GetChunksByPacienteIdAsync(int pacienteId)
        {
            return await _dbSet
                .AsNoTracking()
                .Where(e => e.PatientId == pacienteId)
                .ToListAsync();
        }

        public async Task DeleteChunksByPacienteIdAsync(int pacienteId)
        {
            var chunks = await _dbSet.Where(e => e.PatientId == pacienteId).ToListAsync();
            _dbSet.RemoveRange(chunks);
        }
    }
}
