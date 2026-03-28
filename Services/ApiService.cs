using NutriFlow.Models;

namespace NutriFlow.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;

        public ApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // Pacientes
        public async Task<List<Paciente>?> GetPacientesAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<Paciente>>("pacientes");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao buscar pacientes: {ex.Message}");
                return null;
            }
        }

        public async Task<Paciente?> GetPacienteAsync(int id)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<Paciente>($"pacientes/{id}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao buscar paciente {id}: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> CreatePacienteAsync(Paciente paciente)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("pacientes", paciente);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao criar paciente: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdatePacienteAsync(int id, Paciente paciente)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"pacientes/{id}", paciente);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao atualizar paciente {id}: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeletePacienteAsync(int id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"pacientes/{id}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao excluir paciente {id}: {ex.Message}");
                return false;
            }
        }

        // Planos de Dieta
        public async Task<List<PlanoDieta>?> GetPlanosDietaAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<PlanoDieta>>("planos-dieta");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao buscar planos de dieta: {ex.Message}");
                return null;
            }
        }

        public async Task<List<PlanoDieta>?> GetPlanosDietaByPacienteAsync(int pacienteId)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<PlanoDieta>>($"pacientes/{pacienteId}/planos-dieta");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao buscar planos de dieta do paciente {pacienteId}: {ex.Message}");
                return null;
            }
        }

        // Sessões
        public async Task<List<Sessao>?> GetSessoesAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<Sessao>>("sessoes");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao buscar sessões: {ex.Message}");
                return null;
            }
        }

        public async Task<List<Sessao>?> GetSessoesByPacienteAsync(int pacienteId)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<Sessao>>($"pacientes/{pacienteId}/sessoes");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao buscar sessões do paciente {pacienteId}: {ex.Message}");
                return null;
            }
        }

        // Progresso
        public async Task<List<Progresso>?> GetProgressosAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<Progresso>>("progresso");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao buscar progressos: {ex.Message}");
                return null;
            }
        }

        public async Task<List<Progresso>?> GetProgressosByPacienteAsync(int pacienteId)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<Progresso>>($"pacientes/{pacienteId}/progresso");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao buscar progressos do paciente {pacienteId}: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> CreateProgressoAsync(Progresso progresso)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("progresso", progresso);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao criar progresso: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateProgressoAsync(int id, Progresso progresso)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"progresso/{id}", progresso);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao atualizar progresso {id}: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteProgressoAsync(int id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"progresso/{id}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao excluir progresso {id}: {ex.Message}");
                return false;
            }
        }

        // Planos de Dieta - CRUD completo
        public async Task<PlanoDieta?> GetPlanoDietaAsync(int id)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<PlanoDieta>($"planos-dieta/{id}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao buscar plano de dieta {id}: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> CreatePlanoDietaAsync(PlanoDieta plano)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("planos-dieta", plano);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao criar plano de dieta: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdatePlanoDietaAsync(int id, PlanoDieta plano)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"planos-dieta/{id}", plano);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao atualizar plano de dieta {id}: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeletePlanoDietaAsync(int id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"planos-dieta/{id}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao excluir plano de dieta {id}: {ex.Message}");
                return false;
            }
        }

        // Sessões - CRUD completo
        public async Task<Sessao?> GetSessaoAsync(int id)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<Sessao>($"sessoes/{id}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao buscar sessão {id}: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> CreateSessaoAsync(Sessao sessao)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("sessoes", sessao);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao criar sessão: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateSessaoAsync(int id, Sessao sessao)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"sessoes/{id}", sessao);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao atualizar sessão {id}: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteSessaoAsync(int id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"sessoes/{id}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao excluir sessão {id}: {ex.Message}");
                return false;
            }
        }

        // Assistente IA
        public async Task<string?> GerarPlanoDietaIAAsync(int pacienteId)
        {
            try
            {
                var response = await _httpClient.PostAsync($"ai/gerar-plano/{pacienteId}", null);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao gerar plano com IA: {ex.Message}");
                return null;
            }
        }

        public async Task<string?> EnviarMensagemIAAsync(string mensagem, int? pacienteId = null)
        {
            try
            {
                var request = new { mensagem, pacienteId };
                var response = await _httpClient.PostAsJsonAsync("ai/chat", request);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao enviar mensagem para IA: {ex.Message}");
                return null;
            }
        }
    }
}
