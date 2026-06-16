# NutrIA — Sistema Web de Dietas Personalizadas com IA e RAG

Bem-vindo ao **NutrIA** (NutriFlow), uma plataforma web inovadora baseada no ecossistema C# .NET 8.0, projetada para revolucionar o acompanhamento clínico nutricional através de Inteligência Artificial e a técnica de Geração Aumentada de Recuperação (**RAG — Retrieval-Augmented Generation**).

---

## 🚀 Funcionalidades Principais

* **Acompanhamento Clínico Integrado:** Prontuário eletrônico completo com cadastro de pacientes, sessões de retorno, registros de progresso físico e planos dietéticos.
* **Assistente Inteligente (RAG):** Interface de chat que consulta de forma semântica e contextualizada o histórico do paciente ativo para esclarecer dúvidas e recomendar ajustes alimentares.
* **Busca Semântica Híbrida:** Integração local com **FAISS (IndexFlatL2)** e fallback automático de **Cosseno** para recuperação ultrarrápida de embeddings clínicos.
* **Privacidade Orientada à LGPD:** Pseudonimização automática local de dados sensíveis antes de qualquer tráfego para APIs de modelos externos.
* **Auditoria Abrangente:** Gravação rigorosa de logs de auditoria de consultas da IA (usuário, paciente, pergunta, fontes citadas e latência).
* **Monitoramento Operacional:** Métricas de performance exportadas nativamente via Prometheus com dashboards dinâmicos do Grafana.

---

## 📖 Documentação Completa do TCC / Projeto Integrador

Para fins acadêmicos e avaliação de bancas examinadoras de TCC, geramos uma documentação arquitetural e de engenharia de software abrangente e detalhada em:

👉 **[Documentação Técnica Detalhada (TCC)](DOCUMENTACAO_TECNICA.md)**

A documentação aborda em profundidade:
1. **Visão Geral do Projeto:** Problema, público-alvo, benefícios e diferenciais.
2. **Arquitetura da Solução:** Componentes de software, fluxos de dados e banco de dados.
3. **Pipeline CI/CD:** Automação detalhada de builds, testes unitários, testes frontend, SonarCloud e deploys automatizados em AWS EC2.
4. **Segurança de Software:** Autenticação por Cookies JWT HttpOnly/Strict, proteção CSP, sanitização e Bcrypt.
5. **Observabilidade e Monitoramento:** Estrutura de logs com Serilog, Prometheus metrics e Grafana.
6. **Fluxo RAG & Guardrails:** Pseudonimização, inclusões clínicas forçadas e combate a alucinações.
7. **Cálculo de Peso e IMC:** Fórmulas matemáticas aplicadas e visualização em MudCharts.
8. **Decisões Arquiteturais (ADRs):** Justificativa detalhada de tecnologias e metodologias.
9. **Avaliação Qualitativa nos Critérios da Banca:** Nota estimada detalhada sob escala Likert acadêmica.

---

## 🛠️ Tecnologias Utilizadas

* **Runtime:** C# .NET 8.0 (ASP.NET Core Web API / Minimal APIs)
* **Frontend:** Blazor Interactive Server & MudBlazor v8.15.0
* **Banco de Dados:** Microsoft SQL Server & EF Core 8.0
* **Biblioteca Vetorial:** FAISS (`FaissMask`) & `System.Numerics.Tensors`
* **Inteligência Artificial:** OpenAI API (`gpt-4o-mini` & `text-embedding-3-small`)
* **Logging & Telemetria:** Serilog & prometheus-net.AspNetCore
* **Pipeline e Infra:** GitHub Actions, Docker, Docker Compose, AWS EC2, Prometheus & Grafana.
