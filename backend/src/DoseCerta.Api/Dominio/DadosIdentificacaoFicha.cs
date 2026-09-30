// Copyright (c) Grupo G07 - DoseCerta. Todos os direitos reservados.

namespace DoseCerta.Api.Dominio;

/// <summary>
/// Dados de identificação comuns a qualquer ficha (F002 e F003).
/// </summary>
/// <param name="NomeCompleto">Nome completo do utente.</param>
/// <param name="DataNascimento">Data de nascimento do utente.</param>
/// <param name="ContactoEmergenciaNome">Nome do contacto de emergência.</param>
/// <param name="ContactoEmergenciaTelefone">Telefone do contacto de emergência.</param>
/// <param name="GrauAutonomiaId">Grau de autonomia e mobilidade.</param>
public record DadosIdentificacaoFicha(
    string NomeCompleto,
    DateOnly DataNascimento,
    string ContactoEmergenciaNome,
    string ContactoEmergenciaTelefone,
    int GrauAutonomiaId);
