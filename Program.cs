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
using NutriFlow.Repositories;
using NutriFlow.Health;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi.Models;
using Serilog;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// ─── Observabilidade (Serilog) ─────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

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
var connectionString = GetConnectionString(builder.Configuration);
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// ─── Camada de Repositório (Repository Layer) ─────────────────────────────
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IPacienteRepository, PacienteRepository>();
builder.Services.AddScoped<ISessaoRepository, SessaoRepository>();
builder.Services.AddScoped<IProgressoRepository, ProgressoRepository>();
builder.Services.AddScoped<IPlanoDietaRepository, PlanoDietaRepository>();
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
builder.Services.AddScoped<IEmbeddingChunkRepository, EmbeddingChunkRepository>();

// ─── Camada de Serviços (Service Layer) ───────────────────────────────────
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<IAuthService>(sp => sp.GetRequiredService<AuthService>());
builder.Services.AddScoped<AuditoriaService>();
builder.Services.AddScoped<IAuditoriaService>(sp => sp.GetRequiredService<AuditoriaService>());
builder.Services.AddScoped<NavigationService>();
builder.Services.AddScoped<IPacienteService, PacienteService>();
builder.Services.AddScoped<ISessaoService, SessaoService>();
builder.Services.AddScoped<IProgressoService, ProgressoService>();
builder.Services.AddScoped<IPlanoDietaService, PlanoDietaService>();

// ─── Observabilidade (Health Checks) ───────────────────────────────────────
builder.Services.AddHealthChecks()
    .AddCheck<DbHealthCheck>("Database");

// ─── Segurança (CORS) ─────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("SecurePolicy", policy =>
    {
        policy.WithOrigins("http://localhost:8080", "https://localhost:8080")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// AIService como Singleton: mantém índice em memória entre requests
var openaiApiKey = GetOpenAiApiKey(builder.Configuration);
builder.Services.AddSingleton<AIService>(sp => new AIService(
    openaiApiKey,
    sp.GetRequiredService<IServiceScopeFactory>(),
    sp.GetRequiredService<IConfiguration>()
));

// BackgroundService de re-indexação incremental
builder.Services.AddSingleton<IndexacaoBackgroundService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<IndexacaoBackgroundService>());

// ─── Autenticação JWT ─────────────────────────────────────────────────────
var jwtSettings = builder.Configuration.GetSection("Jwt");
var jwtKey = GetJwtKey(builder.Configuration);
var key = Encoding.UTF8.GetBytes(jwtKey);
var expireHours = int.Parse(jwtSettings["ExpireHours"] ?? "24");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options => ConfigureJwt(options, key, jwtSettings));

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
builder.Services.AddSwaggerGen(c => ConfigureSwagger(c));

builder.Services.AddControllers();

var app = builder.Build();

// ─── Pipeline HTTP ─────────────────────────────────────────────────────────
ConfigurePipeline(app);

// ─── Endpoints de Autenticação ────────────────────────────────────────────
app.MapPost("/api/v1/auth/login", async (
    HttpContext httpContext,
    [Microsoft.AspNetCore.Mvc.FromForm] string email,
    [Microsoft.AspNetCore.Mvc.FromForm] string senha,
    IAuthService authService) => await HandleLoginAsync(httpContext, email, senha, authService, expireHours)).DisableAntiforgery();

app.MapGet("/api/v1/auth/logout", (HttpContext httpContext) => HandleLogout(httpContext));

// ─── Endpoints RAG ────────────────────────────────────────────────────────
app.MapPost("/api/v1/assistente/query", async (
    HttpContext httpContext,
    AssistenteQuery request,
    AIService aiService,
    IAuditoriaService auditoriaService,
    IPacienteService pacienteService) => await HandleQueryAsync(httpContext, request, aiService, auditoriaService, pacienteService))
.RequireAuthorization().DisableAntiforgery().RequireRateLimiting("ia-policy");

app.MapGet("/api/v1/assistente/audit/{pacienteId:int}", async (
    int pacienteId,
    HttpContext httpContext,
    IAuditoriaService auditoriaService,
    IPacienteService pacienteService) => await HandleAuditAsync(pacienteId, httpContext, auditoriaService, pacienteService))
.RequireAuthorization();

app.MapGet("/api/v1/assistente/metricas", async (
    HttpContext httpContext,
    IAuditoriaService auditoriaService) => await HandleMetricasAsync(httpContext, auditoriaService))
.RequireAuthorization();

// ─── Endpoints de Versionamento e Observabilidade ──────────────────────────
app.MapHealthChecks("/health");

app.MapGet("/api/v1/version", () => Results.Ok(new {
    Version = "1.1.0",
    Status = "Active",
    Environment = app.Environment.EnvironmentName,
    Framework = ".NET 8.0"
}));

// ─── Migrações Automáticas ────────────────────────────────────────────────
await ApplyMigrationsAsync(app);

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

// ─── Funções Auxiliares Locais ─────────────────────────────────────────────

static string GetConnectionString(IConfiguration configuration)
{
    var connectionString = configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

    var dbSecret = configuration["DbSettings:Secret"];
    if (!string.IsNullOrEmpty(dbSecret))
    {
        connectionString = connectionString.Replace("SecretKeyPlaceholder=True;", $"Password={dbSecret};");
    }

    if (Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") != "true")
    {
        connectionString = connectionString.Replace("host.docker.internal", "localhost");
    }

    return connectionString;
}

static string GetOpenAiApiKey(IConfiguration configuration)
{
    return Environment.GetEnvironmentVariable("OPENAI_API_KEY")
        ?? configuration["OpenAI:ApiKey"]
        ?? throw new InvalidOperationException(
            "OpenAI API Key not found. Set OPENAI_API_KEY environment variable or configure OpenAI:ApiKey in appsettings.json");
}

static string GetJwtKey(IConfiguration configuration)
{
    return Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
        ?? configuration.GetValue<string>("Jwt:Key")
        ?? throw new InvalidOperationException("JWT secret key not configured. Set JWT_SECRET_KEY env var or use dotnet user-secrets.");
}

static void ConfigureJwt(JwtBearerOptions options, byte[] key, IConfigurationSection jwtSettings)
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
}

static void ConfigureSwagger(Swashbuckle.AspNetCore.SwaggerGen.SwaggerGenOptions c)
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
}

static void ConfigurePipeline(WebApplication app)
{
    app.UseCors("SecurePolicy");
    
    app.Use(async (context, next) =>
    {
        context.Response.Headers.Append("Content-Security-Policy", "default-src 'self'; style-src 'self' https://fonts.googleapis.com; font-src 'self' https://fonts.gstatic.com; img-src 'self' data:; script-src 'self'; frame-ancestors 'none';");
        context.Response.Headers.Append("X-Frame-Options", "DENY");
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
        context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
        context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
        await next();
    });

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
}

static IResult HandleLogout(HttpContext httpContext)
{
    httpContext.Response.Cookies.Delete("NutriAI.AuthToken");
    return Results.Redirect("/");
}

static async Task ApplyMigrationsAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    var db = services.GetRequiredService<ApplicationDbContext>();
    
    for (int i = 1; i <= 12; i++)
    {
        try
        {
            logger.LogInformation("Aplicando migrações do banco de dados (Tentativa {Tentativa}/12)...", i);
            await db.Database.MigrateAsync();
            logger.LogInformation("Banco de dados pronto e migrações aplicadas com sucesso!");
            break;
        }
        catch (Exception ex)
        {
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
            await Task.Delay(5000);
        }
    }
}

static async Task<IResult> HandleLoginAsync(
    HttpContext httpContext,
    string email,
    string senha,
    IAuthService authService,
    int expireHours)
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
}

static async Task<IResult> HandleQueryAsync(
    HttpContext httpContext,
    AssistenteQuery request,
    AIService aiService,
    IAuditoriaService auditoriaService,
    IPacienteService pacienteService)
{
    if (!httpContext.User.Identity?.IsAuthenticated ?? true)
        return Results.Unauthorized();

    var userIdClaim = httpContext.User.FindFirst("UsuarioId")?.Value;
    if (!int.TryParse(userIdClaim, out int userId))
        return Results.Unauthorized();

    if (!request.ConsentimentoLGPD)
        return Results.BadRequest(new { error = "Consentimento LGPD obrigatório para consultas à IA." });

    var paciente = await pacienteService.GetPacienteByIdAsync(request.PacienteId, userId);

    if (paciente == null)
        return Results.Forbid();

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
}

static async Task<IResult> HandleAuditAsync(
    int pacienteId,
    HttpContext httpContext,
    IAuditoriaService auditoriaService,
    IPacienteService pacienteService)
{
    if (!httpContext.User.Identity?.IsAuthenticated ?? true)
        return Results.Unauthorized();

    var userIdClaim = httpContext.User.FindFirst("UsuarioId")?.Value;
    if (!int.TryParse(userIdClaim, out int userId))
        return Results.Unauthorized();

    var paciente = await pacienteService.GetPacienteByIdAsync(pacienteId, userId);

    if (paciente == null)
        return Results.Forbid();

    var logs = await auditoriaService.GetLogsPorPacienteAsync(pacienteId, userId.ToString());
    return Results.Ok(logs);
}

static async Task<IResult> HandleMetricasAsync(
    HttpContext httpContext,
    IAuditoriaService auditoriaService)
{
    if (!httpContext.User.Identity?.IsAuthenticated ?? true)
        return Results.Unauthorized();

    var userIdClaim = httpContext.User.FindFirst("UsuarioId")?.Value;
    if (string.IsNullOrEmpty(userIdClaim)) return Results.Unauthorized();

    var metricas = await auditoriaService.GetMetricasAsync(userIdClaim);
    return Results.Ok(metricas);
}

namespace NutriFlow.Health
{
    public class DbHealthCheck : IHealthCheck
    {
        private readonly ApplicationDbContext _db;
        public DbHealthCheck(ApplicationDbContext db) => _db = db;
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _db.Database.CanConnectAsync(cancellationToken)
                    ? HealthCheckResult.Healthy("Conectividade do Banco de Dados OK")
                    : HealthCheckResult.Unhealthy("Conectividade do Banco de Dados Falhou");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy(ex.Message);
            }
        }
    }
}
