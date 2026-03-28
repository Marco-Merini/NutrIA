namespace NutriFlow.Services
{
    public class NavigationService
    {
        public event Action<string>? PageChanged;

        public void NavigateToDashboard()
        {
            PageChanged?.Invoke("/");
        }

        public void NavigateToPacientes()
        {
            PageChanged?.Invoke("/pacientes");
        }

        public void NavigateToPacienteDetalhes(int pacienteId)
        {
            PageChanged?.Invoke($"/pacientes/{pacienteId}");
        }

        public void NavigateToDietas()
        {
            PageChanged?.Invoke("/dietas");
        }

        public void NavigateToPlanoDetalhes(int planoId)
        {
            PageChanged?.Invoke($"/dietas/{planoId}");
        }

        public void NavigateToSessoes()
        {
            PageChanged?.Invoke("/sessoes");
        }

        public void NavigateToProgresso()
        {
            PageChanged?.Invoke("/progresso");
        }

        public void NavigateToAssistenteIA()
        {
            PageChanged?.Invoke("/assistente-ia");
        }
    }
}
