/// Consulta e pede as autorizações de que os avisos dependem (modelo de classes, secção 7; RF-062, RNF-041): as
/// notificações e os alarmes exatos. Cada método devolve `null` quando o estado não se consegue saber.
abstract interface class PermissaoNotificacoes {
  /// Indica se as notificações da aplicação estão autorizadas.
  Future<bool?> estado();

  /// Pede a autorização das notificações (a janela do sistema ou, se já foi recusada, as definições da aplicação).
  Future<bool?> pedir();

  /// Indica se a aplicação pode agendar alarmes exatos.
  Future<bool?> estadoAlarmesExatos();

  /// Abre as definições onde o Utilizador autoriza os alarmes exatos.
  Future<bool?> pedirAlarmesExatos();
}
