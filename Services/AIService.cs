using OpenAI;
using OpenAI.Chat;
using OpenAI.Embeddings;
using System.Numerics.Tensors;
using FaissMask;
using NutriFlow.Models;
using System.Text;
using System.Text.Json;

namespace NutriFlow.Services
{
    public class DocumentChunk
    {
        public int Id { get; set; }
        public string Source { get; set; } = "";
        public string Text { get; set; } = "";
        public float[] Embedding { get; set; } = Array.Empty<float>();
    }

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

    public class AIService
    {
        private readonly ChatClient _chatClient;
        private readonly EmbeddingClient _embeddingClient;
        private const int EmbeddingDimension = 1536; // text-embedding-3-small = 1536 dimensões

        // Armazenamento em memória dos chunks do paciente indexado
        public List<DocumentChunk> IndexedChunks { get; private set; } = new();
        public int? CurrentIndexedPacienteId { get; private set; }

        public AIService(string apiKey)
        {
            var credential = new System.ClientModel.ApiKeyCredential(apiKey);
            _chatClient = new ChatClient("gpt-4o-mini", credential);
            _embeddingClient = new EmbeddingClient("text-embedding-3-small", credential);
        }

        /// <summary>
        /// Indexa os dados em lote de um paciente específico
        /// </summary>
        public async Task IndexPacienteAsync(Paciente paciente)
        {
            CurrentIndexedPacienteId = paciente.Id; // Identifica que é uma base específica
            IndexedChunks.Clear();

            var allRawChunks = GenerateChunksFromPaciente(paciente);

            if (allRawChunks.Count == 0) return;

            // Processa em lotes de 50 para evitar limitação da API
            for (int i = 0; i < allRawChunks.Count; i += 50)
            {
                var batch = allRawChunks.Skip(i).Take(50).ToList();
                var texts = batch.Select(c => c.Text).ToList();

                try
                {
                    var embeddingResult = await _embeddingClient.GenerateEmbeddingsAsync(texts);

                    for (int j = 0; j < batch.Count; j++)
                    {
                        batch[j].Id = IndexedChunks.Count;
                        batch[j].Embedding = embeddingResult.Value[j].ToFloats().ToArray();
                        IndexedChunks.Add(batch[j]);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[AIService] Falha na indexação do lote {i}: {ex.Message}");
                    throw new Exception($"Falha de comunicação com a OpenAI (Lote {i}): {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Responde uma pergunta usando RAG: embedding da query → vector search FAISS → LLM com contexto.
        /// </summary>
        public async Task<string> AskQuestionAsync(string question)
        {
            if (IndexedChunks.Count == 0)
            {
                return "O histórico do paciente ainda não foi indexado. Por favor, selecione um paciente e clique em 'Indexar Selecionado' primeiro.";
            }

            // 1. Gerar embedding da pergunta
            var questionEmbeddingResult = await _embeddingClient.GenerateEmbeddingAsync(question);
            var questionVector = questionEmbeddingResult.Value.ToFloats().ToArray();

            // 2. Vector Search (FAISS com fallback para Cosine Similarity)
            var topChunks = SearchSimilarChunks(questionVector, topK: 5);

            // 3. Montar contexto RAG
            var contextBuilder = new StringBuilder();
            foreach (var chunk in topChunks)
            {
                contextBuilder.AppendLine($"--- INFORMAÇÃO ({chunk.Source}):");
                contextBuilder.AppendLine(chunk.Text);
                contextBuilder.AppendLine();
            }

            var systemPrompt = $@"Você é um Nutricionista Especialista de uma IA chamada NutrIA.
Seu objetivo é auxiliar o nutricionista lendo o histórico vetorizado do paciente e respondendo à pergunta usando ESTRITAMENTE as informações fornecidas abaixo.
Responda de forma clara, objetiva e profissional em português brasileiro.
Caso a informação não esteja no contexto, diga que não há registro no histórico do paciente.

CONTEXTO RECUPERADO VIA BUSCA VETORIAL (FAISS):
{contextBuilder}";

            // 4. Chamar o LLM com contexto
            var messages = new List<ChatMessage>
            {
                ChatMessage.CreateSystemMessage(systemPrompt),
                ChatMessage.CreateUserMessage(question)
            };

            var completion = await _chatClient.CompleteChatAsync(messages);
            return completion.Value.Content[0].Text ?? "Não foi possível gerar uma resposta.";
        }

        /// <summary>
        /// Gera um plano alimentar personalizado com base nos dados do usuário usando a IA.
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

            var userData = $"Nome: {paciente.Nome}\nIdade: {(DateTime.Now.Year - (paciente.DataNascimento?.Year ?? DateTime.Now.Year))}\nSexo: {paciente.Sexo}\nPeso: {paciente.PesoAtual}kg\nAltura: {paciente.Altura}m\nObjetivo: {paciente.Objetivo}\nNível Atividade: {paciente.NivelAtividade}\nPreferências: {paciente.PreferenciasAlimentares}\nCondições Saúde: {paciente.CondicoesSaude}\nMedicamentos: {paciente.Medicamentos}\nObservações: {paciente.Observacoes}";

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
                Console.WriteLine($"Erro ao desserializar JSON da IA: {ex.Message}\nJSON retornado: {jsonResponse}");
                return null;
            }
        }

        /// <summary>
        /// Realiza busca vetorial usando FAISS (IndexFlatL2) com fallback para Cosine Similarity via System.Numerics.Tensors.
        /// </summary>
        private List<DocumentChunk> SearchSimilarChunks(float[] queryVector, int topK)
        {
            topK = Math.Min(topK, IndexedChunks.Count);
            if (topK == 0) return new List<DocumentChunk>();

            try
            {
                // FAISS - Facebook AI Similarity Search (conforme RFC)
                using var index = new IndexFlatL2(EmbeddingDimension);

                // Preparar todos os vetores em um array flat (n * d)
                var allVectors = new float[IndexedChunks.Count * EmbeddingDimension];
                for (int i = 0; i < IndexedChunks.Count; i++)
                    Array.Copy(IndexedChunks[i].Embedding, 0, allVectors, i * EmbeddingDimension, EmbeddingDimension);

                index.Add(allVectors);

                // Buscar os topK mais similares
                var searchResults = index.Search(queryVector, topK);

                var results = new List<DocumentChunk>();
                foreach (var result in searchResults)
                {
                    if (result.Label >= 0 && result.Label < IndexedChunks.Count)
                        results.Add(IndexedChunks[(int)result.Label]);
                }

                if (results.Any()) return results;
            }
            catch (Exception ex)
            {
                // Fallback seguro caso a biblioteca nativa faiss_c não esteja disponível no ambiente
                // Usa System.Numerics.Tensors (Cosine Similarity) como método alternativo
                Console.WriteLine($"[AIService] FAISS indisponível, usando fallback Cosine Similarity: {ex.Message}");
            }

            // Fallback: Cosine Similarity via System.Numerics.Tensors
            return IndexedChunks
                .OrderByDescending(c => TensorPrimitives.CosineSimilarity(queryVector, c.Embedding))
                .Take(topK)
                .ToList();
        }

        /// <summary>
        /// Gera os chunks de texto a partir dos dados do paciente (perfil, sessões, progresso, planos de dieta).
        /// </summary>
        private List<DocumentChunk> GenerateChunksFromPaciente(Paciente p)
        {
            var chunks = new List<DocumentChunk>();
            var pref = $"[{p.Nome}] ";

            // Perfil Básico
            chunks.Add(new DocumentChunk
            {
                Source = $"{pref}Perfil do Paciente",
                Text = $"Paciente: {p.Nome}, Data de Nascimento: {p.DataNascimento:dd/MM/yyyy}, Sexo: {p.Sexo}, Peso Atual: {p.PesoAtual}kg, Altura: {p.Altura}m. Objetivo: {p.Objetivo}. Nível de Atividade: {p.NivelAtividade}."
            });

            if (!string.IsNullOrWhiteSpace(p.PreferenciasAlimentares))
                chunks.Add(new DocumentChunk { Source = $"{pref}Preferências Alimentares", Text = $"Preferências alimentares do paciente {p.Nome}: {p.PreferenciasAlimentares}" });

            if (!string.IsNullOrWhiteSpace(p.CondicoesSaude))
                chunks.Add(new DocumentChunk { Source = $"{pref}Condições de Saúde", Text = $"Condições de saúde declaradas pela paciente {p.Nome}: {p.CondicoesSaude}" });

            if (!string.IsNullOrWhiteSpace(p.Medicamentos))
                chunks.Add(new DocumentChunk { Source = $"{pref}Medicamentos", Text = $"Medicamentos em uso pelo paciente {p.Nome}: {p.Medicamentos}" });

            if (!string.IsNullOrWhiteSpace(p.Observacoes))
                chunks.Add(new DocumentChunk { Source = $"{pref}Observações Gerais", Text = $"Observações sobre o paciente {p.Nome}: {p.Observacoes}" });

            // Sessões
            if (p.Sessoes != null)
            {
                foreach (var s in p.Sessoes)
                {
                    chunks.Add(new DocumentChunk
                    {
                        Source = $"{pref}Sessão em {s.DataSessao:dd/MM/yyyy}",
                        Text = $"Sessão do tipo '{s.Tipo}' realizada com paciente {p.Nome} em {s.DataSessao:dd/MM/yyyy}. Peso na sessão: {s.PesoSessao}kg. Anotações do nutricionista: {s.Anotacoes}. Próxima consulta agendada: {s.ProximaConsulta:dd/MM/yyyy}."
                    });
                }
            }

            // Progresso
            if (p.Progressos != null)
            {
                foreach (var prog in p.Progressos)
                {
                    chunks.Add(new DocumentChunk
                    {
                        Source = $"{pref}Progresso em {prog.DataRegistro:dd/MM/yyyy}",
                        Text = $"Registro de progresso do paciente {p.Nome} em {prog.DataRegistro:dd/MM/yyyy}. Peso: {prog.Peso}kg. Cintura: {prog.CinturaCm}cm. Quadril: {prog.QuadrilCm}cm. Gordura corporal: {prog.PercentualGordura}%. Aderência ao plano: {prog.AderenciaPlano}%. Humor: {prog.Humor}. Energia: {prog.Energia}. Feedback: {prog.Feedback}."
                    });
                }
            }

            // Planos de Dieta
            if (p.PlanosDieta != null)
            {
                foreach (var plano in p.PlanosDieta)
                {
                    chunks.Add(new DocumentChunk
                    {
                        Source = $"{pref}Plano de Dieta - {plano.Titulo}",
                        Text = $"Plano de dieta '{plano.Titulo}' criado para {p.Nome} em {plano.DataCriacao:dd/MM/yyyy}. Objetivo: {plano.Objetivo}. Calorias diárias: {plano.CaloriasDiarias}kcal. Macros: Proteínas {plano.ProteinasG}g, Carboidratos {plano.CarboidratosG}g, Gorduras {plano.GordurasG}g. Orientações: {plano.Orientacoes}. Observações do nutricionista: {plano.ObservacoesNutricionista}."
                    });
                }
            }

            return chunks;
        }
    }
}
