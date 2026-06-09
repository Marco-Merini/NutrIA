using NutriFlow.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NutriFlow.Repositories
{
    public interface IEmbeddingChunkRepository : IRepository<EmbeddingChunk>
    {
        Task<List<EmbeddingChunk>> GetChunksByPacienteIdAsync(int pacienteId);
        Task DeleteChunksByPacienteIdAsync(int pacienteId);
    }
}
