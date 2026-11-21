/// Estado de um convite (RF-045, RF-046).
enum EstadoConvite {
  /// À espera de resposta; só estes aparecem na área de convites (RF-042).
  pendente('Pendente'),

  /// Aceite: o convidado ficou associado à ficha (RF-045).
  aceite('Aceite'),

  /// Recusado: não foi criada associação (RF-046).
  recusado('Recusado');

  const EstadoConvite(this.valor);

  /// Valor usado pela API.
  final String valor;

  /// Lê o valor da API.
  static EstadoConvite fromJson(Object? valor) =>
      EstadoConvite.values.firstWhere(
        (estado) => estado.valor == valor,
        orElse: () =>
            throw FormatException('Estado de convite desconhecido: $valor'),
      );
}
