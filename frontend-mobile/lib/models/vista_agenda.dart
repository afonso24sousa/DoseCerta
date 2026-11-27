/// Vista da agenda (OP-45, parâmetro `vista`; RF-084 a RF-086).
enum VistaAgenda {
  /// Eventos de um dia, por hora (RF-084).
  dia('dia', 'Dia'),

  /// Eventos da semana de segunda-feira a domingo (RF-085).
  semana('semana', 'Semana'),

  /// Eventos do mês (RF-086).
  mes('mes', 'Mês');

  const VistaAgenda(this.valor, this.rotulo);

  /// Valor usado pela API.
  final String valor;

  /// Texto apresentado.
  final String rotulo;
}
