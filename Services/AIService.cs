using OpenAI;
using OpenAI.Chat;
using OpenAI.Embeddings;
using System.Numerics.Tensors;
using FaissMask;
using NutriFlow.Models;
using NutriFlow.Models.Rag;
using NutriFlow.Data;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace NutriFlow.Services
{
    // ─────────────────────────────────────────────────────────────────────────
    // DTOs mantidos para retrocompatibilidade com NovoPlanoAlimentar.razor
    // ─────────────────────────────────────────────────────────────────────────

    public class GeneratedDietPlan
    {
        public string? Titulo { get; set; }
        public string? Objetivo { get; set; }
        public decimal? CaloriasDiarias { get; set; }
        public decimal? ProteinasG { get; set; }
        public decimal? CarboidratosG { get; set; }
        public decimal? GordurasG { get; set; }
        public string? Orientacoes { get; set; }
        public string? ObservacoesNutricionista { get; set; }
        public List<GeneratedRefeicao> Refeicoes { get; set; } = new();
    }

    public class GeneratedRefeicao
    {
        public string? Nome { get; set; }
        public string? HorarioTexto { get; set; }
        public string? Alimentos { get; set; }
        public int? Calorias { get; set; }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // AIService — Singleton com índice em memória + persistência no SQL Server
    // ─────────────────────────────────────────────────────────────────────────

    public class AIService
    {
        private readonly ChatClient _chatClient;
        private readonly EmbeddingClient _embeddingClient;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _configuration;

        private const int EmbeddingDimension = 1536; // text-embedding-3-small

        /// <summary>
        /// Índice em memória por pacienteId. Permite múltiplos pacientes indexados simultaneamente
        /// sem sobrescrever o índice de outro usuário concorrente.
        /// </summary>
        private readonly Dictionary<int, List<DocumentChunk>> _patientIndexes = new();
        private readonly SemaphoreSlim _indexLock = new(1, 1);

        /// <summary>Id do último paciente indexado (retrocompatibilidade).</summary>
        public int? CurrentIndexedPacienteId { get; private set; }

        /// <summary>
        /// Chunks do paciente atual em memória (retrocompatibilidade com AssistenteIA.razor).
        /// </summary>
        public List<DocumentChunk> IndexedChunks =>
            CurrentIndexedPacienteId.HasValue && _patientIndexes.TryGetValue(CurrentIndexedPacienteId.Value, out var c)
                ? c
                : new List<DocumentChunk>();

        public AIService(string apiKey, IServiceScopeFactory scopeFactory, IConfiguration configuration)
        {
            var credential = new System.ClientModel.ApiKeyCredential(apiKey);
            var ragSection = configuration.GetSection("RAG");
            var chatModel = ragSection["ChatModel"] ?? "gpt-4o-mini";
            var embeddingModel = ragSection["EmbeddingModel"] ?? "text-embedding-3-small";

            _chatClient = new ChatClient(chatModel, credential);
            _embeddingClient = new EmbeddingClient(embeddingModel, credential);
            _scopeFactory = scopeFactory;
            _configuration = configuration;
        }

        // ─────────────────────────────────────────────────────────────────────
        // INDEXAÇÃO
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Tenta carregar o índice de um paciente a partir do banco de dados (embeddings persistidos).
        /// Retorna true se encontrou e carregou embeddings existentes; false se precisa re-indexar.
        /// </summary>
        public async Task<bool> LoadIndexFromDbAsync(int pacienteId)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var storedChunks = await db.Embeddings
                .AsNoTracking()
                .Where(e => e.PatientId == pacienteId)
                .ToListAsync();

            if (!storedChunks.Any()) return false;

            var chunks = storedChunks.Select((e, i) => new DocumentChunk
            {
                Id = i,
                Source = $"{e.SourceTable}:{e.SourceId}",
                Tabela = e.SourceTable,
                RegistroId = e.SourceId,
                PacienteId = e.PatientId,
                Tipo = e.ChunkType,
                DataAtualizacao = e.SourceUpdatedAt,
                Text = e.ChunkText,
                Embedding = JsonSerializer.Deserialize<float[]>(e.EmbeddingJson) ?? Array.Empty<float>()
            }).ToList();

            await _indexLock.WaitAsync();
            try
            {
                _patientIndexes[pacienteId] = chunks;
                CurrentIndexedPacienteId = pacienteId;
            }
            finally
            {
                _indexLock.Release();
            }

            Console.WriteLine($"[AIService] Índice do paciente {pacienteId} carregado do banco: {chunks.Count} chunks.");
            return true;
        }

        /// <summary>
        /// Indexa os dados de um paciente: gera chunks, embeddings e persiste no SQL Server.
        /// Re-indexação completa: remove embeddings antigos e substitui pelos novos.
        /// </summary>
        public async Task IndexPacienteAsync(Paciente paciente)
        {
            var allRawChunks = GenerateChunksFromPaciente(paciente);
            if (allRawChunks.Count == 0) return;

            // Gera embeddings em lotes de 50 (limite da API OpenAI)
            for (int i = 0; i < allRawChunks.Count; i += 50)
            {
                var batch = allRawChunks.Skip(i).Take(50).ToList();
                var texts = batch.Select(c => c.Text).ToList();

                try
                {
                    var embeddingResult = await _embeddingClient.GenerateEmbeddingsAsync(texts);
                    for (int j = 0; j < batch.Count; j++)
                    {
                        batch[j].Id = i + j;
                        batch[j].Embedding = embeddingResult.Value[j].ToFloats().ToArray();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[AIService] Falha na indexação do lote {i}: {ex.Message}");
                    throw new Exception($"Falha de comunicação com a OpenAI (Lote {i}): {ex.Message}", ex);
                }
            }

            // Atualiza índice em memória
            await _indexLock.WaitAsync();
            try
            {
                _patientIndexes[paciente.Id] = allRawChunks;
                CurrentIndexedPacienteId = paciente.Id;
            }
            finally
            {
                _indexLock.Release();
            }

            // Persiste no SQL Server em background (não bloqueia o caller)
            _ = Task.Run(() => PersistEmbeddingsAsync(paciente.Id, allRawChunks));
        }

        /// <summary>
        /// Persiste os embeddings de um paciente no SQL Server.
        /// Remove os embeddings antigos e insere os novos (upsert completo).
        /// </summary>
        private async Task PersistEmbeddingsAsync(int pacienteId, List<DocumentChunk> chunks)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // Remove embeddings antigos do paciente
                var old = await db.Embeddings.Where(e => e.PatientId == pacienteId).ToListAsync();
                db.Embeddings.RemoveRange(old);

                // Insere novos
                var entities = chunks.Select(c => new EmbeddingChunk
                {
                    PatientId = pacienteId,
                    SourceTable = c.Tabela,
                    SourceId = c.RegistroId,
                    ChunkType = c.Tipo,
                    ChunkText = c.Text,
                    EmbeddingJson = JsonSerializer.Serialize(c.Embedding),
                    SourceUpdatedAt = c.DataAtualizacao,
                    CreatedAt = DateTime.UtcNow
                }).ToList();

                db.Embeddings.AddRange(entities);
                await db.SaveChangesAsync();

                Console.WriteLine($"[AIService] {chunks.Count} embeddings do paciente {pacienteId} persistidos no banco.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AIService] Erro ao persistir embeddings: {ex.Message}");
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // RAG — Consulta com citação de fontes (pipeline clínico completo)
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Pipeline RAG completo: embedding da query → busca vetorial → prompt clínico → LLM.
        /// Retorna resposta com citações de fontes e metadados de auditoria.
        /// </summary>
        public async Task<AssistenteResponse> AskQuestionRagAsync(
            string question,
            Paciente paciente,
            RagOptions? options = null)
        {
            options ??= BuildDefaultOptions();
            var sw = System.Diagnostics.Stopwatch.StartNew();

            // Garantir que o índice está carregado
            if (!_patientIndexes.TryGetValue(paciente.Id, out var chunks) || chunks.Count == 0)
            {
                return new AssistenteResponse
                {
                    Answer = "⚠️ O histórico deste paciente ainda não foi indexado. " +
                             "Por favor, clique em 'Indexar Paciente' antes de fazer perguntas.",
                    UsouDadosIndexados = false
                };
            }

            // 1. Gerar embedding da query (pseudonimizada se necessário)
            float[] queryVector;
            try
            {
                var qResult = await _embeddingClient.GenerateEmbeddingAsync(question);
                queryVector = qResult.Value.ToFloats().ToArray();
            }
            catch (Exception ex)
            {
                return new AssistenteResponse
                {
                    Error = $"Falha ao gerar embedding da query: {ex.Message}"
                };
            }

            // 2. Busca vetorial top-K com score threshold
            var topChunks = SearchSimilarChunks(queryVector, chunks, options.TopK, options.ScoreThreshold);

            // Forçar a inclusão de chunks essenciais do paciente para contornar falhas de busca semântica (multi-hop / gaps)
            var forceChunks = new List<DocumentChunk>();

            // a. Chunks de perfil e dados clínicos principais
            var coreTypes = new[] { "perfil", "preferencias", "saude", "medicamentos", "observacoes" };
            var coreChunks = chunks.Where(c => coreTypes.Contains(c.Tipo)).ToList();
            forceChunks.AddRange(coreChunks);

            // b. Plano de dieta ativo ou mais recente
            var activePlano = chunks.FirstOrDefault(c => c.Tipo == "plano" && 
                (c.Text.Contains("Status: ativo", StringComparison.OrdinalIgnoreCase) || 
                 c.Text.Contains("Status: Ativo", StringComparison.OrdinalIgnoreCase)));
            
            if (activePlano == null)
            {
                activePlano = chunks
                    .Where(c => c.Tipo == "plano")
                    .OrderByDescending(c => c.DataAtualizacao)
                    .FirstOrDefault();
            }

            if (activePlano != null)
            {
                forceChunks.Add(activePlano);
                
                // c. Refeições associadas a este plano
                if (activePlano.RegistroId.HasValue)
                {
                    int planoId = activePlano.RegistroId.Value;
                    var activeRefeicoes = chunks
                        .Where(c => c.Tipo == "refeicao" && c.Text.Contains($"Plano ID={planoId}"))
                        .ToList();
                    forceChunks.AddRange(activeRefeicoes);
                }
            }

            // Mesclar evitando duplicidade
            var finalChunks = new List<DocumentChunk>(topChunks);
            foreach (var fc in forceChunks)
            {
                if (!finalChunks.Any(c => c.Tabela == fc.Tabela && c.RegistroId == fc.RegistroId))
                {
                    // Calcula score de similaridade real para exibição se tiver o embedding
                    if (fc.Embedding != null && fc.Embedding.Length > 0)
                    {
                        fc.Score = TensorPrimitives.CosineSimilarity(queryVector, fc.Embedding);
                    }
                    finalChunks.Add(fc);
                }
            }

            topChunks = finalChunks;

            if (!topChunks.Any())
            {
                return new AssistenteResponse
                {
                    Answer = "Não encontrei documentos relevantes no histórico indexado para responder esta pergunta. " +
                             "Por favor, reformule sua pergunta ou verifique se o paciente possui dados suficientes.",
                    UsouDadosIndexados = false,
                    LatenciaMs = (int)sw.ElapsedMilliseconds
                };
            }

            // 3. Construir prompt clínico com perfil pseudonimizado + documentos recuperados
            var patientProfile = BuildPseudonymizedProfile(paciente, options);
            var contextBlock = BuildContextBlock(topChunks);

            var systemPrompt = BuildSystemPrompt();
            var userContent = BuildUserContent(patientProfile, contextBlock, question, paciente.Nome, options);

            // 4. Chamar o LLM
            string answer;
            try
            {
                var messages = new List<ChatMessage>
                {
                    ChatMessage.CreateSystemMessage(systemPrompt),
                    ChatMessage.CreateUserMessage(userContent)
                };

                var ragSection = _configuration.GetSection("RAG");
                int maxTokens = int.TryParse(ragSection["MaxTokensResposta"], out var mt) ? mt : 2000;

                var completionOptions = new ChatCompletionOptions { MaxOutputTokenCount = maxTokens };
                var completion = await _chatClient.CompleteChatAsync(messages, completionOptions);
                answer = completion.Value.Content[0].Text ?? "Não foi possível gerar uma resposta.";
            }
            catch (Exception ex)
            {
                return new AssistenteResponse
                {
                    Error = $"Falha ao chamar o LLM: {ex.Message}",
                    LatenciaMs = (int)sw.ElapsedMilliseconds
                };
            }

            sw.Stop();

            // 5. Estruturar fontes únicas e retrieved chunks
            var sources = topChunks
                .GroupBy(c => $"{c.Tabela}:{c.RegistroId}")
                .Select(g => new SourceReference
                {
                    Tabela = g.First().Tabela,
                    RegistroId = g.First().RegistroId ?? 0,
                    Descricao = g.First().Source,
                    Citacao = $"[{g.First().Tabela}:{g.First().RegistroId}]"
                })
                .ToList();

            var retrieved = topChunks.Select(c => new RetrievedChunk
            {
                Source = c.Source,
                Tabela = c.Tabela,
                RegistroId = c.RegistroId,
                Tipo = c.Tipo,
                Score = c.Score,
                TextPreview = c.Text.Length > 300 ? c.Text[..300] + "…" : c.Text
            }).ToList();

            return new AssistenteResponse
            {
                Answer = answer,
                Sources = sources,
                Retrieved = retrieved,
                LatenciaMs = (int)sw.ElapsedMilliseconds,
                UsouDadosIndexados = true
            };
        }

        /// <summary>
        /// Retrocompatibilidade: responde pergunta simples sem retornar metadados de fontes.
        /// Usado pela versão anterior da UI (AssistenteIA.razor antes da refatoração).
        /// </summary>
        public async Task<string> AskQuestionAsync(string question)
        {
            if (IndexedChunks.Count == 0)
                return "O histórico do paciente ainda não foi indexado. Por favor, selecione um paciente e clique em 'Indexar Paciente' primeiro.";

            var qResult = await _embeddingClient.GenerateEmbeddingAsync(question);
            var queryVector = qResult.Value.ToFloats().ToArray();

            var topChunks = SearchSimilarChunks(queryVector, IndexedChunks, 5, 0.0f);

            var contextBuilder = new StringBuilder();
            foreach (var chunk in topChunks)
                contextBuilder.AppendLine($"--- [{chunk.Tabela}:{chunk.RegistroId}]:\n{chunk.Text}\n");

            var systemPrompt = BuildSystemPrompt();
            var messages = new List<ChatMessage>
            {
                ChatMessage.CreateSystemMessage(systemPrompt),
                ChatMessage.CreateUserMessage($"Contexto:\n{contextBuilder}\n\nPergunta: {question}")
            };

            var completion = await _chatClient.CompleteChatAsync(messages);
            return completion.Value.Content[0].Text ?? "Não foi possível gerar uma resposta.";
        }

        // ─────────────────────────────────────────────────────────────────────
        // GERAÇÃO DE PLANO ALIMENTAR (retrocompatibilidade)
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Gera plano alimentar em JSON estruturado. Usado por NovoPlanoAlimentar.razor.
        /// </summary>
        public async Task<GeneratedDietPlan?> GenerateDietPlanAsync(Paciente paciente)
        {
            var systemPrompt = @"Você é um Nutricionista Especialista de uma IA chamada NutrIA.
Seu objetivo é criar um plano alimentar completo e personalizado baseado nos dados do paciente fornecidos.
Retorne EXCLUSIVAMENTE um objeto JSON com a seguinte estrutura (sem formatação markdown, apenas o texto do JSON):
{
  ""Titulo"": ""Nome do Plano"",
  ""Objetivo"": ""Objetivo do Plano"",
  ""CaloriasDiarias"": 2000,
  ""ProteinasG"": 150,
  ""CarboidratosG"": 200,
  ""GordurasG"": 60,
  ""Orientacoes"": ""Orientações gerais de hidratação e afins"",
  ""ObservacoesNutricionista"": ""Observações clínicas"",
  ""Refeicoes"": [
    {
      ""Nome"": ""Café da Manhã"",
      ""HorarioTexto"": ""07:00"",
      ""Alimentos"": ""2 fatias de pão integral, 2 ovos mexidos..."",
      ""Calorias"": 400
    }
  ]
}";

            var userData = $"Paciente ID: {paciente.Id}\nIdade: {CalcularIdade(paciente.DataNascimento)}\nSexo: {paciente.Sexo}\nPeso: {paciente.PesoAtual}kg\nAltura: {paciente.Altura}m\nObjetivo: {paciente.Objetivo}\nNível Atividade: {paciente.NivelAtividade}\nPreferências: {paciente.PreferenciasAlimentares}\nCondições Saúde: {paciente.CondicoesSaude}\nMedicamentos: {paciente.Medicamentos}\nObservações: {paciente.Observacoes}";

            var messages = new List<ChatMessage>
            {
                ChatMessage.CreateSystemMessage(systemPrompt),
                ChatMessage.CreateUserMessage(userData)
            };

            var options = new ChatCompletionOptions { ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat() };
            var completion = await _chatClient.CompleteChatAsync(messages, options);
            var jsonResponse = completion.Value.Content[0].Text;

            try
            {
                return JsonSerializer.Deserialize<GeneratedDietPlan>(jsonResponse);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AIService] Erro ao desserializar JSON da IA: {ex.Message}\nJSON: {jsonResponse}");
                return null;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // CHUNKING — Geração de chunks a partir dos dados relacionais
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Gera chunks de texto normalizados a partir de todas as entidades do paciente.
        /// Aplica pseudonimização LGPD: remove nome, e-mail e telefone dos chunks enviados ao LLM.
        /// Inclui: Pacientes, Sessoes, Progresso, Planos_Dieta, Refeicoes.
        /// </summary>
        public List<DocumentChunk> GenerateChunksFromPaciente(Paciente p)
        {
            var chunks = new List<DocumentChunk>();

            // ── Perfil básico (pseudonimizado — sem nome, e-mail, telefone) ──
            chunks.Add(new DocumentChunk
            {
                Tabela = "Pacientes",
                RegistroId = p.Id,
                PacienteId = p.Id,
                Tipo = "perfil",
                DataAtualizacao = p.DataAtualizacao,
                Source = $"Pacientes:{p.Id}",
                Text = $"Paciente ID={p.Id}. Idade: {CalcularIdade(p.DataNascimento)}. Sexo: {p.Sexo ?? "N/I"}. " +
                       $"Peso atual: {p.PesoAtual}kg. Altura: {p.Altura}m. " +
                       $"Objetivo: {p.Objetivo ?? "N/I"}. Nível de atividade: {p.NivelAtividade ?? "N/I"}. " +
                       $"Status: {p.Status ?? "N/I"}. Cadastro: {p.DataCadastro:dd/MM/yyyy}."
            });

            if (!string.IsNullOrWhiteSpace(p.PreferenciasAlimentares))
                chunks.Add(new DocumentChunk
                {
                    Tabela = "Pacientes", RegistroId = p.Id, PacienteId = p.Id,
                    Tipo = "preferencias", DataAtualizacao = p.DataAtualizacao,
                    Source = $"Pacientes:{p.Id}",
                    Text = $"Preferências alimentares do paciente ID={p.Id}: {p.PreferenciasAlimentares}."
                });

            if (!string.IsNullOrWhiteSpace(p.CondicoesSaude))
                chunks.Add(new DocumentChunk
                {
                    Tabela = "Pacientes", RegistroId = p.Id, PacienteId = p.Id,
                    Tipo = "saude", DataAtualizacao = p.DataAtualizacao,
                    Source = $"Pacientes:{p.Id}",
                    Text = $"Condições de saúde do paciente ID={p.Id}: {p.CondicoesSaude}."
                });

            if (!string.IsNullOrWhiteSpace(p.Medicamentos))
                chunks.Add(new DocumentChunk
                {
                    Tabela = "Pacientes", RegistroId = p.Id, PacienteId = p.Id,
                    Tipo = "medicamentos", DataAtualizacao = p.DataAtualizacao,
                    Source = $"Pacientes:{p.Id}",
                    Text = $"Medicamentos em uso pelo paciente ID={p.Id}: {p.Medicamentos}."
                });

            if (!string.IsNullOrWhiteSpace(p.Observacoes))
                chunks.Add(new DocumentChunk
                {
                    Tabela = "Pacientes", RegistroId = p.Id, PacienteId = p.Id,
                    Tipo = "observacoes", DataAtualizacao = p.DataAtualizacao,
                    Source = $"Pacientes:{p.Id}",
                    Text = $"Observações clínicas do paciente ID={p.Id}: {p.Observacoes}."
                });

            // ── Sessões ──
            foreach (var s in p.Sessoes ?? new List<Sessao>())
            {
                chunks.Add(new DocumentChunk
                {
                    Tabela = "Sessoes", RegistroId = s.Id, PacienteId = p.Id,
                    Tipo = "sessao", DataAtualizacao = s.DataAtualizacao,
                    Source = $"Sessoes:{s.Id}",
                    Text = $"Sessão ID={s.Id} | Tipo: '{s.Tipo}' | Data: {s.DataSessao:dd/MM/yyyy}. " +
                           $"Peso na sessão: {s.PesoSessao}kg. " +
                           $"Anotações do nutricionista: {s.Anotacoes ?? "nenhuma"}. " +
                           $"Próxima consulta agendada: {s.ProximaConsulta:dd/MM/yyyy}."
                });
            }

            // ── Progressos ──
            foreach (var prog in p.Progressos ?? new List<Progresso>())
            {
                chunks.Add(new DocumentChunk
                {
                    Tabela = "Progresso", RegistroId = prog.Id, PacienteId = p.Id,
                    Tipo = "progresso", DataAtualizacao = prog.DataAtualizacao,
                    Source = $"Progresso:{prog.Id}",
                    Text = $"Progresso ID={prog.Id} | Data: {prog.DataRegistro:dd/MM/yyyy}. " +
                           $"Peso: {prog.Peso}kg. Cintura: {prog.CinturaCm}cm. Quadril: {prog.QuadrilCm}cm. " +
                           $"Gordura corporal: {prog.PercentualGordura}%. " +
                           $"Aderência ao plano: {prog.AderenciaPlano ?? "N/I"}. " +
                           $"Humor: {prog.Humor ?? "N/I"}. Energia: {prog.Energia ?? "N/I"}. " +
                           $"Feedback: {prog.Feedback ?? "nenhum"}."
                });
            }

            // ── Planos de Dieta + Refeições ──
            foreach (var plano in p.PlanosDieta ?? new List<PlanoDieta>())
            {
                chunks.Add(new DocumentChunk
                {
                    Tabela = "Planos_Dieta", RegistroId = plano.Id, PacienteId = p.Id,
                    Tipo = "plano", DataAtualizacao = plano.DataAtualizacao,
                    Source = $"Planos_Dieta:{plano.Id}",
                    Text = $"Plano de Dieta ID={plano.Id} | Título: '{plano.Titulo}' | " +
                           $"Criado em: {plano.DataCriacao:dd/MM/yyyy}. " +
                           $"Objetivo: {plano.Objetivo ?? "N/I"}. " +
                           $"Calorias diárias: {plano.CaloriasDiarias}kcal. " +
                           $"Macros — Proteínas: {plano.ProteinasG}g | Carboidratos: {plano.CarboidratosG}g | Gorduras: {plano.GordurasG}g. " +
                           $"Orientações: {plano.Orientacoes ?? "N/I"}. " +
                           $"Obs nutricionista: {plano.ObservacoesNutricionista ?? "N/I"}. " +
                           $"Status: {plano.Status ?? "N/I"}. Gerado por IA: {(plano.GeradoPorIa == true ? "sim" : "não")}."
                });

                // ── Refeições do plano (ausentes na versão anterior) ──
                foreach (var r in plano.Refeicoes ?? new List<Refeicao>())
                {
                    chunks.Add(new DocumentChunk
                    {
                        Tabela = "Refeicoes", RegistroId = r.Id, PacienteId = p.Id,
                        Tipo = "refeicao", DataAtualizacao = r.DataCriacao,
                        Source = $"Refeicoes:{r.Id}",
                        Text = $"Refeição ID={r.Id} | Nome: '{r.Nome}' | " +
                               $"Plano ID={plano.Id} ('{plano.Titulo}'). " +
                               $"Horário: {r.Horario ?? "N/I"}. " +
                               $"Alimentos: {r.Alimentos ?? "N/I"}. " +
                               $"Calorias: {r.Calorias}kcal."
                    });
                }
            }

            return chunks;
        }

        // ─────────────────────────────────────────────────────────────────────
        // BUSCA VETORIAL
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Busca vetorial: tenta FAISS (IndexFlatL2), com fallback para Cosine Similarity.
        /// Aplica score threshold para filtrar resultados irrelevantes.
        /// </summary>
        private List<DocumentChunk> SearchSimilarChunks(
            float[] queryVector,
            List<DocumentChunk> chunks,
            int topK,
            float scoreThreshold)
        {
            topK = Math.Min(topK, chunks.Count);
            if (topK == 0) return new List<DocumentChunk>();

            // ── Tentativa FAISS ──
            try
            {
                using var index = new IndexFlatL2(EmbeddingDimension);
                for (int i = 0; i < chunks.Count; i++)
                {
                    index.Add(chunks[i].Embedding);
                }

                var searchResults = index.Search(queryVector, topK);

                var results = new List<DocumentChunk>();
                foreach (var result in searchResults)
                {
                    if (result.Label >= 0 && result.Label < chunks.Count)
                    {
                        var chunk = chunks[(int)result.Label];
                        // Converter distância L2 para score aproximado (quanto menor distância, maior score)
                        chunk.Score = Math.Max(0f, 1f - (result.Distance / 4f));
                        results.Add(chunk);
                    }
                }

                if (results.Any()) return results.Where(r => r.Score >= scoreThreshold).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AIService] FAISS indisponível, usando Cosine Similarity: {ex.Message}");
            }

            // ── Fallback: Cosine Similarity via System.Numerics.Tensors ──
            return chunks
                .Select(c =>
                {
                    c.Score = TensorPrimitives.CosineSimilarity(queryVector, c.Embedding);
                    return c;
                })
                .Where(c => c.Score >= scoreThreshold)
                .OrderByDescending(c => c.Score)
                .Take(topK)
                .ToList();
        }

        // ─────────────────────────────────────────────────────────────────────
        // PROMPT ENGINEERING
        // ─────────────────────────────────────────────────────────────────────

        private static string BuildSystemPrompt() => """
            Você é um assistente profissional em nutrição chamado NutrIA.

            REGRAS OBRIGATÓRIAS:
            1. Responda SOMENTE com base nos documentos recuperados fornecidos abaixo.
            2. Cite a fonte de CADA afirmação usando o formato [Tabela:Id] — exemplo: [Planos_Dieta:45], [Progresso:200], [Refeicoes:78].
            3. Se a informação não estiver nos documentos recuperados, responda claramente: "Não tenho informação suficiente nos registros indexados para responder isto."
            4. NUNCA invente dados clínicos, valores nutricionais ou histórico que não esteja nas fontes.
            5. Ao gerar planos alimentares, respeite SEMPRE: alergias, condições de saúde, medicamentos e preferências do paciente.
            6. Responda em português brasileiro, de forma clara, profissional e estruturada.
            """;

        private static string BuildUserContent(
            string patientProfile,
            string contextBlock,
            string question,
            string? patientName,
            RagOptions options)
        {
            var sb = new StringBuilder();
            sb.AppendLine("## Perfil do Paciente (pseudonimizado para proteção LGPD)");
            sb.AppendLine(patientProfile);
            sb.AppendLine();

            if (!string.IsNullOrWhiteSpace(patientName))
            {
                sb.AppendLine("## Mapeamento de Identidade (Sessão Segura)");
                sb.AppendLine($"- O paciente selecionado nesta sessão chama-se '{patientName}'.");
                sb.AppendLine($"- Trate qualquer referência a '{patientName}' (ou variações como '{patientName.ToLower()}') na pergunta como referindo-se a este paciente (ID do perfil).");
                sb.AppendLine($"- Toda e qualquer informação sobre o perfil, plano de dieta, refeições e histórico deste paciente refere-se a '{patientName}'.");
                sb.AppendLine();
            }

            sb.AppendLine("## Documentos Recuperados (busca vetorial semântica)");
            sb.AppendLine(contextBlock);

            if (options.MetaCaloriasDia.HasValue || options.MetaPlano != null || options.PreferenciasExtras != null)
            {
                sb.AppendLine();
                sb.AppendLine("## Parâmetros da Solicitação");
                if (options.MetaCaloriasDia.HasValue)
                    sb.AppendLine($"- Meta calórica: {options.MetaCaloriasDia}kcal/dia");
                if (options.MetaPlano != null)
                    sb.AppendLine($"- Meta do plano: {options.MetaPlano}");
                if (options.NumRefeicoesDia > 0)
                    sb.AppendLine($"- Refeições por dia: {options.NumRefeicoesDia} refeições + {options.NumLanchesDia} lanches");
                if (!string.IsNullOrWhiteSpace(options.PreferenciasExtras))
                    sb.AppendLine($"- Preferências adicionais: {options.PreferenciasExtras}");
            }

            sb.AppendLine();
            sb.AppendLine($"## Pergunta");
            sb.AppendLine(question);

            return sb.ToString();
        }

        private static string BuildContextBlock(List<DocumentChunk> chunks)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < chunks.Count; i++)
            {
                var c = chunks[i];
                sb.AppendLine($"[{c.Tabela}:{c.RegistroId}] (score: {c.Score:F2}, tipo: {c.Tipo})");
                sb.AppendLine(c.Text);
                sb.AppendLine();
            }
            return sb.ToString();
        }

        // ─────────────────────────────────────────────────────────────────────
        // PSEUDONIMIZAÇÃO LGPD
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Constrói perfil do paciente com dados clínicos relevantes,
        /// omitindo dados pessoais identificáveis (nome, e-mail, telefone) — conformidade LGPD.
        /// </summary>
        private static string BuildPseudonymizedProfile(Paciente p, RagOptions? options = null)
        {
            var objetivo = options?.MetaPlano ?? p.Objetivo ?? "não informado";
            return $"ID: {p.Id} | Idade: {CalcularIdade(p.DataNascimento)} | Sexo: {p.Sexo ?? "N/I"} | " +
                   $"Peso: {p.PesoAtual}kg | Altura: {p.Altura}m | IMC: {CalcularImc(p.PesoAtual, p.Altura):F1} | " +
                   $"Objetivo: {objetivo} | Nível atividade: {p.NivelAtividade ?? "N/I"} | " +
                   $"Preferências alimentares: {p.PreferenciasAlimentares ?? "N/I"} | " +
                   $"Condições de saúde: {p.CondicoesSaude ?? "N/I"} | " +
                   $"Medicamentos: {p.Medicamentos ?? "N/I"} | " +
                   $"Observações: {p.Observacoes ?? "N/I"}";
        }

        // ─────────────────────────────────────────────────────────────────────
        // HELPERS
        // ─────────────────────────────────────────────────────────────────────

        private static string CalcularIdade(DateTime? dataNascimento)
        {
            if (!dataNascimento.HasValue) return "não informada";
            var age = DateTime.Now.Year - dataNascimento.Value.Year;
            if (DateTime.Now.DayOfYear < dataNascimento.Value.DayOfYear) age--;
            return $"{age} anos";
        }

        private static decimal CalcularImc(decimal? peso, decimal? altura)
        {
            if (!peso.HasValue || !altura.HasValue || altura.Value == 0) return 0;
            return peso.Value / (altura.Value * altura.Value);
        }

        private RagOptions BuildDefaultOptions()
        {
            var section = _configuration.GetSection("RAG");
            return new RagOptions
            {
                TopK = int.TryParse(section["TopK"], out var k) ? k : 8,
                ScoreThreshold = float.TryParse(section["ScoreThreshold"],
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var t) ? t : 0.25f
            };
        }

        /// <summary>Retorna resumo do índice de um paciente por tipo de chunk.</summary>
        public Dictionary<string, int> GetIndexSummary(int pacienteId)
        {
            if (!_patientIndexes.TryGetValue(pacienteId, out var chunks))
                return new Dictionary<string, int>();

            return chunks
                .GroupBy(c => c.Tipo)
                .ToDictionary(g => g.Key, g => g.Count());
        }

        /// <summary>Remove o índice em memória de um paciente (ex: após exclusão).</summary>
        public void RemoveIndex(int pacienteId)
        {
            _patientIndexes.Remove(pacienteId);
            if (CurrentIndexedPacienteId == pacienteId)
                CurrentIndexedPacienteId = null;
        }

        /// <summary>Verifica se um paciente está indexado em memória.</summary>
        public bool IsIndexed(int pacienteId) => _patientIndexes.ContainsKey(pacienteId);
    }
}
