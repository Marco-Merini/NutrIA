using System.Threading.Channels;
using NutriFlow.Data;
using NutriFlow.Services;
using Microsoft.EntityFrameworkCore;

namespace NutriFlow.Services
{
    /// <summary>
    /// Background service que processa re-indexação incremental de pacientes em uma fila assíncrona.
    /// Ao criar/atualizar/excluir dados de um paciente, enfileire o pacienteId via EnquileirarReindexacao().
    /// O serviço processa a fila em background, carregando o paciente do banco e re-indexando via AIService.
    /// </summary>
    public class IndexacaoBackgroundService : BackgroundService
    {
        private readonly Channel<IndexacaoRequest> _channel;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<IndexacaoBackgroundService> _logger;

        /// <summary>Canal de comunicação thread-safe para enfileirar re-indexações.</summary>
        private readonly ChannelWriter<IndexacaoRequest> _writer;
        private readonly ChannelReader<IndexacaoRequest> _reader;

        public IndexacaoBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<IndexacaoBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;

            // Canal bounded: máximo 50 requisições pendentes para evitar sobrecarga
            _channel = Channel.CreateBounded<IndexacaoRequest>(new BoundedChannelOptions(50)
            {
                FullMode = BoundedChannelFullMode.DropOldest, // descarta a mais antiga se cheio
                SingleReader = true,
                SingleWriter = false
            });

            _writer = _channel.Writer;
            _reader = _channel.Reader;
        }

        /// <summary>
        /// Enfileira a re-indexação de um paciente.
        /// Deve ser chamado após criar/atualizar/excluir dados de um paciente.
        /// Thread-safe: pode ser chamado de qualquer contexto.
        /// </summary>
        /// <param name="pacienteId">Id do paciente a re-indexar.</param>
        /// <param name="motivo">Motivo da re-indexação (para log).</param>
        public void EnquileirarReindexacao(int pacienteId, string motivo = "atualização")
        {
            var request = new IndexacaoRequest(pacienteId, motivo, DateTime.UtcNow);

            if (!_writer.TryWrite(request))
            {
                _logger.LogWarning(
                    "[IndexacaoBackground] Canal cheio — re-indexação do paciente {PacienteId} descartada.",
                    pacienteId);
            }
            else
            {
                _logger.LogInformation(
                    "[IndexacaoBackground] Re-indexação enfileirada: paciente {PacienteId} — motivo: {Motivo}.",
                    pacienteId, motivo);
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("[IndexacaoBackground] Serviço de re-indexação incremental iniciado.");

            await foreach (var request in _reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    await ProcessarReindexacaoAsync(request, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "[IndexacaoBackground] Erro ao processar re-indexação do paciente {PacienteId}.",
                        request.PacienteId);
                }
            }

            _logger.LogInformation("[IndexacaoBackground] Serviço de re-indexação encerrado.");
        }

        private async Task ProcessarReindexacaoAsync(IndexacaoRequest request, CancellationToken ct)
        {
            _logger.LogInformation(
                "[IndexacaoBackground] Iniciando re-indexação do paciente {PacienteId} (motivo: {Motivo}).",
                request.PacienteId, request.Motivo);

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var aiService = scope.ServiceProvider.GetRequiredService<AIService>();

            // Carrega paciente com todas as relações necessárias
            var paciente = await db.Pacientes
                .AsNoTracking()
                .Include(p => p.Sessoes)
                .Include(p => p.Progressos)
                .Include(p => p.PlanosDieta)
                    .ThenInclude(pd => pd.Refeicoes)
                .FirstOrDefaultAsync(p => p.Id == request.PacienteId, ct);

            if (paciente == null)
            {
                _logger.LogWarning(
                    "[IndexacaoBackground] Paciente {PacienteId} não encontrado — re-indexação ignorada.",
                    request.PacienteId);
                return;
            }

            await aiService.IndexPacienteAsync(paciente);

            _logger.LogInformation(
                "[IndexacaoBackground] Re-indexação do paciente {PacienteId} concluída. Chunks: {Count}.",
                request.PacienteId, aiService.GetIndexSummary(request.PacienteId).Values.Sum());
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _writer.Complete();
            await base.StopAsync(cancellationToken);
        }
    }

    /// <summary>Requisição de re-indexação enfileirada.</summary>
    public record IndexacaoRequest(int PacienteId, string Motivo, DateTime EnfileiradoEm);
}
