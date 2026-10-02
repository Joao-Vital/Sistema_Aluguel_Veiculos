using LocadoraVeiculos.API.Data;
using LocadoraVeiculos.API.Middleware;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ---------- Serviços ----------
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Evita erro de referência circular caso alguma entidade seja serializada diretamente (fallback de segurança)
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

// Padroniza o corpo de erro 400 quando a validação de ModelState (Data Annotations) falha
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var erros = context.ModelState
            .Where(e => e.Value != null && e.Value.Errors.Count > 0)
            .ToDictionary(
                e => e.Key,
                e => e.Value!.Errors.Select(err => err.ErrorMessage).ToArray()
            );

        return new BadRequestObjectResult(new
        {
            status = 400,
            titulo = "Um ou mais campos são inválidos.",
            erros
        });
    };
});

// Contexto do EF Core apontando para o SQL Server Express
builder.Services.AddDbContext<LocadoraContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("LocadoraConnection")));

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "API - Locadora de Veículos",
        Version = "v1",
        Description = "Sistema de aluguel de veículos (C#, Entity Framework, SQL Server Express)"
    });
});

var app = builder.Build();

// ---------- Pipeline HTTP ----------

// Tratamento global de exceções (deve vir antes de tudo)
app.UseExceptionHandling();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Locadora de Veículos API v1");
        c.RoutePrefix = string.Empty; // Swagger abre direto na raiz ao rodar (F5)
    });
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
