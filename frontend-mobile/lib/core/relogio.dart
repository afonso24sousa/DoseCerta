/// Instante atual, numa interface que os testes podem substituir.
abstract interface class Relogio {
  /// Devolve o instante atual.
  DateTime agora();
}

/// Relógio do sistema.
class RelogioSistema implements Relogio {
  /// Cria o relógio do sistema.
  const RelogioSistema();

  @override
  DateTime agora() => DateTime.now();
}
