// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using System.Text.Json.Serialization;
using DoseCerta.Api.Dados;
using DoseCerta.Api.Infraestrutura;
using DoseCerta.Api.Servicos;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<DoseCertaDbContext>(opcoes =>
    opcoes.UseSqlServer(builder.Configuration.GetConnectionString("DoseCerta")));

// A emissão dos tokens (registo e login) pertence à I001; aqui apenas são validados.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
builder.Services.AddAuthorization();

builder.Services.Configure<OpcoesArmazenamento>(builder.Configuration.GetSection(OpcoesArmazenamento.Seccao));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IArmazenamentoDocumentos, ArmazenamentoDocumentosLocal>();
builder.Services.AddScoped<IVerificadorDependenciasFicha, VerificadorDependenciasFicha>();
builder.Services.AddScoped<IServicoFichas, ServicoFichas>();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<TratadorExcecoes>();
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
    o.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "DoseCerta.Api.xml")));

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    await DadosDesenvolvimento.PrepararAsync(app.Services);
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

await app.RunAsync();

/// <summary>
/// Ponto de entrada da API (exposto para os testes de integração).
/// </summary>
public partial class Program
{
}
