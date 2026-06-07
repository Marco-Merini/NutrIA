using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MudBlazor;
using MudBlazor.Services;
using NutriFlow.Components;
using NutriFlow.Data;
using NutriFlow.Models.Rag;
using NutriFlow.Services;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ─── Blazor ───────────────────────────────────────────────────────────────
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.TopCenter;
    config.SnackbarConfiguration.RequireInteraction = false;
    config.SnackbarConfiguration.PreventDuplicates = true;
    config.SnackbarConfiguration.NewestOnTop = false;
    config.SnackbarConfiguration.ShowCloseIcon = false;
    config.SnackbarConfiguration.VisibleStateDuration = 2000;
    config.SnackbarConfiguration.HideTransitionDuration = 500;
    config.SnackbarConfiguration.ShowTransitionDuration = 500;
    config.SnackbarConfiguration.SnackbarVariant = Variant.Outlined;
});

// ─── Banco de Dados ───────────────────────────────────────────────────────
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

if (Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") != "true")
{
    connectionString = connectionString.Replace("host.docker.internal", "localhost");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// ─── Serviços de Aplicação ────────────────────────────────────────────────
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<NavigationService>();
builder.Services.AddScoped<AuditoriaService>();
builder.Services.AddScoped<IPacienteService, PacienteService>();

// AIService como Singleton: mantém índice em memória entre requests
var openaiApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
    ?? builder.Configuration["OpenAI:ApiKey"]
    ?? throw new InvalidOperationException(
        "OpenAI API Key not found. Set OPENAI_API_KEY environment variable or configure OpenAI:ApiKey in appsettings.json");
builder.Services.AddSingleton<AIService>(sp => new AIService(
    openaiApiKey,
    sp.GetRequiredService<IServiceScopeFactory>(),
    sp.GetRequiredService<IConfiguration>()
));

// BackgroundService de re-indexação incremental
// Registrado como Singleton para permitir injeção direta em outros serviços
builder.Services.AddSingleton<IndexacaoBackgroundService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<IndexacaoBackgroundService>());

// ─── Autenticação JWT ─────────────────────────────────────────────────────
var jwtSettings = builder.Configuration.GetSection("Jwt");
var jwtKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
    ?? builder.Configuration.GetValue<string>("Jwt:Key")
    ?? throw new InvalidOperationException("JWT secret key not configured. Set JWT_SECRET_KEY env var or use dotnet user-secrets.");
var key = Encoding.UTF8.GetBytes(jwtKey);
var expireHours = int.Parse(jwtSettings["ExpireHours"] ?? "24");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };

    // Lê JWT do cookie em vez do header Authorization
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var token = context.Request.Cookies["NutriAI.AuthToken"];
            if (!string.IsNullOrEmpty(token))
                context.Token = token;
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<AuthenticationStateProvider, JwtAuthenticationStateProvider>();
builder.Services.AddCascadingAuthenticationState();

// ─── Rate Limiting ────────────────────────────────────────────────────────
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("ia-policy", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 10; // Max 10 requisições por minuto
        opt.QueueLimit = 2;   // Fila de espera de até 2 requisições
        opt.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
    });
});

// ─── Swagger/OpenAPI ──────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "NutrIA API",
        Version = "v1",
        Description = "API do Sistema Web Gerador de Dietas Personalizadas com IA (NutrIA)"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Autenticação JWT usando o esquema Bearer. Exemplo: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddControllers();

var app = builder.Build();

// ─── Pipeline HTTP ─────────────────────────────────────────────────────────
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "NutrIA API v1");
    });
}

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

// ─── Endpoints de Autenticação ────────────────────────────────────────────
app.MapPost("/api/v1/auth/login", async (
    HttpContext httpContext,
    [Microsoft.AspNetCore.Mvc.FromForm] string email,
    [Microsoft.AspNetCore.Mvc.FromForm] string senha,
    ApplicationDbContext dbContext,
    AuthService authService) =>
{
    var (success, token) = await authService.LoginAsync(email, senha);

    if (success && token != null)
    {
        httpContext.Response.Cookies.Append("NutriAI.AuthToken", token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddHours(expireHours)
        });
        return Results.Redirect("/home");
    }

    return Results.Redirect("/?error=1");
}).DisableAntiforgery();

app.MapGet("/api/v1/auth/logout", (HttpContext httpContext) =>
{
    httpContext.Response.Cookies.Delete("NutriAI.AuthToken");
    return Results.Redirect("/");
});

// ─── Endpoint RAG: POST /api/v1/assistente/query ─────────────────────────────
app.MapPost("/api/v1/assistente/query", async (
    HttpContext httpContext,
    AssistenteQuery request,
    AIService aiService,
    AuditoriaService auditoriaService,
    ApplicationDbContext db) =>
{
    if (!httpContext.User.Identity?.IsAuthenticated ?? true)
        return Results.Unauthorized();

    var userIdClaim = httpContext.User.FindFirst("UsuarioId")?.Value;
    if (!int.TryParse(userIdClaim, out int userId))
        return Results.Unauthorized();

    if (!request.ConsentimentoLGPD)
        return Results.BadRequest(new { error = "Consentimento LGPD obrigatório para consultas à IA." });

    // 4. Verificar que o pacienteId pertence ao userId autenticado (ownership check)
    var paciente = await db.Pacientes
        .AsNoTracking()
        .Include(p => p.Sessoes)
        .Include(p => p.Progressos)
        .Include(p => p.PlanosDieta)
            .ThenInclude(pd => pd.Refeicoes)
        .FirstOrDefaultAsync(p => p.Id == request.PacienteId && p.UsuarioId == userId);

    if (paciente == null)
        return Results.Forbid(); // paciente não existe ou não pertence ao usuário

    // 5. Verificar/carregar índice do paciente (do banco se não estiver em memória)
    if (!aiService.IsIndexed(request.PacienteId))
    {
        var loaded = await aiService.LoadIndexFromDbAsync(request.PacienteId);
        if (!loaded)
        {
            return Results.Ok(new AssistenteResponse
            {
                Answer = "O histórico deste paciente ainda não foi indexado. " +
                         "Por favor, acesse a página Assistente IA e clique em 'Indexar Paciente'.",
                UsouDadosIndexados = false
            });
        }
    }

    var response = await aiService.AskQuestionRagAsync(
        request.Query,
        paciente,
        request.Options);

    await auditoriaService.LogConsultaAsync(
        userId.ToString(),
        request.PacienteId,
        "/api/v1/assistente/query",
        request.Query,
        response,
        request.ConsentimentoLGPD);

    return Results.Ok(response);

}).RequireAuthorization().DisableAntiforgery().RequireRateLimiting("ia-policy");

// ─── Endpoint RAG: GET /api/v1/assistente/audit/{pacienteId} ─────────────────
app.MapGet("/api/v1/assistente/audit/{pacienteId:int}", async (
    int pacienteId,
    HttpContext httpContext,
    AuditoriaService auditoriaService,
    ApplicationDbContext db) =>
{
    if (!httpContext.User.Identity?.IsAuthenticated ?? true)
        return Results.Unauthorized();

    var userIdClaim = httpContext.User.FindFirst("UsuarioId")?.Value;
    if (!int.TryParse(userIdClaim, out int userId))
        return Results.Unauthorized();

    var pacienteExiste = await db.Pacientes
        .AnyAsync(p => p.Id == pacienteId && p.UsuarioId == userId);

    if (!pacienteExiste)
        return Results.Forbid();

    var logs = await auditoriaService.GetLogsPorPacienteAsync(pacienteId, userId.ToString());
    return Results.Ok(logs);

}).RequireAuthorization();

// ─── Endpoint RAG: GET /api/v1/assistente/metricas ───────────────────────────
app.MapGet("/api/v1/assistente/metricas", async (
    HttpContext httpContext,
    AuditoriaService auditoriaService) =>
{
    if (!httpContext.User.Identity?.IsAuthenticated ?? true)
        return Results.Unauthorized();

    var userIdClaim = httpContext.User.FindFirst("UsuarioId")?.Value;
    if (string.IsNullOrEmpty(userIdClaim)) return Results.Unauthorized();

    var metricas = await auditoriaService.GetMetricasAsync(userIdClaim);
    return Results.Ok(metricas);

}).RequireAuthorization();

// ─── Blazor ───────────────────────────────────────────────────────────────
// ─── Migrações Automáticas ────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    var db = services.GetRequiredService<ApplicationDbContext>();
    
    for (int i = 1; i <= 12; i++)
    {
        try
        {
            logger.LogInformation("Aplicando migrações do banco de dados (Tentativa {Tentativa}/12)...", i);
            db.Database.Migrate();
            logger.LogInformation("Banco de dados pronto e migrações aplicadas com sucesso!");
            break;
        }
        catch (Exception ex)
        {
            // Se as tabelas já existem, podemos prosseguir normalmente
            if (ex.Message.Contains("Já existe um objeto com nome") || 
                ex.Message.Contains("already exists") || 
                ex.Message.Contains("there is already an object"))
            {
                logger.LogWarning("O banco de dados já possui a estrutura de tabelas criada. Ignorando migração inicial.");
                break;
            }

            logger.LogWarning("SQL Server não está pronto: {Message}. Aguardando 5 segundos...", ex.Message);
            if (i == 12)
            {
                logger.LogError(ex, "Falha de conexão com o banco após 12 tentativas.");
                throw;
            }
            Thread.Sleep(5000);
        }
    }
}

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
