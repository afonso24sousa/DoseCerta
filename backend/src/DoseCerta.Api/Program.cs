// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

using DoseCerta.Api.Arranque;

var builder = WebApplication.CreateBuilder(args);
ArranqueApi.ConfigurarServicos(builder);

var app = builder.Build();

if (ComandoEmailTeste.Pedido(args, out var destino))
{
    return await ComandoEmailTeste.ExecutarAsync(app.Services, destino);
}

ArranqueApi.ConfigurarPipeline(app);

if (app.Configuration.GetValue<bool>("BaseDados:AplicarMigracoesNoArranque"))
{
    await ArranqueApi.AplicarMigracoesAsync(app);
}

await app.RunAsync();
return 0;
