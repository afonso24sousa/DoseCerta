import 'package:flutter_local_notifications/flutter_local_notifications.dart';
import 'package:timezone/timezone.dart' as tz;

import 'agendador_avisos.dart';
import 'aviso_local.dart';
import 'permissao_notificacoes.dart';

/// Canal Android dos avisos de toma.
const AndroidNotificationDetails _canal = AndroidNotificationDetails(
  'avisos_toma',
  'Avisos de toma',
  channelDescription:
      'Aviso à hora prevista de cada toma das fichas em que é Cuidador.',
  importance: Importance.high,
  priority: Priority.high,
  category: AndroidNotificationCategory.reminder,
);

/// Liga o pacote `flutter_local_notifications` uma só vez, antes do primeiro uso.
class _Plugin {
  _Plugin._();

  static final FlutterLocalNotificationsPlugin plugin =
      FlutterLocalNotificationsPlugin();
  static Future<bool?>? _inicio;

  static Future<FlutterLocalNotificationsPlugin> iniciado() async {
    await (_inicio ??= plugin.initialize(
      settings: const InitializationSettings(
        android: AndroidInitializationSettings('@mipmap/ic_launcher'),
      ),
    ));
    return plugin;
  }

  static Future<AndroidFlutterLocalNotificationsPlugin?> android() async {
    final p = await iniciado();
    return p
        .resolvePlatformSpecificImplementation<
          AndroidFlutterLocalNotificationsPlugin
        >();
  }
}

/// Agendador de avisos com o `flutter_local_notifications` (modelo de classes, secção 7): alarme do sistema
/// operativo, que dispara sem rede e com o ecrã bloqueado e volta a ser agendado depois de reiniciar o telemóvel
/// (RF-062, RNF-041). Com os alarmes exatos autorizados, o aviso usa o modo exato (desvio máximo de 60 segundos);
/// sem essa autorização, o Android pode atrasá-lo.
class AgendadorFlutterLocalNotifications implements AgendadorAvisos {
  /// Cria o agendador.
  const AgendadorFlutterLocalNotifications();

  @override
  Future<void> agendar(AvisoLocal aviso) async {
    final plugin = await _Plugin.iniciado();
    final android = await _Plugin.android();
    final exato = await android?.canScheduleExactNotifications() ?? false;
    // O instante é absoluto (vem com o desvio de Lisboa), por isso agenda-se em UTC e não depende do fuso do
    // telemóvel.
    await plugin.zonedSchedule(
      id: aviso.idAviso,
      title: aviso.titulo,
      body: aviso.corpo,
      scheduledDate: tz.TZDateTime.from(aviso.previstaEm, tz.UTC),
      notificationDetails: const NotificationDetails(android: _canal),
      androidScheduleMode: exato
          ? AndroidScheduleMode.exactAllowWhileIdle
          : AndroidScheduleMode.inexactAllowWhileIdle,
      payload: aviso.fichaId,
    );
  }

  @override
  Future<void> cancelar(int idAviso) async {
    final plugin = await _Plugin.iniciado();
    await plugin.cancel(id: idAviso);
  }

  @override
  Future<void> cancelarTodos() async {
    final plugin = await _Plugin.iniciado();
    await plugin.cancelAll();
  }
}

/// Autorizações do Android pelo `flutter_local_notifications` (modelo de classes, secção 7). Nada é pedido sem um
/// toque do Utilizador, para a janela do sistema nunca aparecer sozinha (nem parar os cenários de sistema do
/// QG-07).
class PermissaoNotificacoesAndroid implements PermissaoNotificacoes {
  /// Cria o acesso às autorizações.
  const PermissaoNotificacoesAndroid();

  @override
  Future<bool?> estado() async {
    final android = await _Plugin.android();
    return android?.areNotificationsEnabled();
  }

  @override
  Future<bool?> pedir() async {
    final android = await _Plugin.android();
    final autorizada = await android?.requestNotificationsPermission();
    if (autorizada == false) {
      // Depois de duas recusas o Android já não mostra a janela: abre as definições de notificações da aplicação.
      await _Plugin.plugin.openAppNotificationSettings();
      return android?.areNotificationsEnabled();
    }
    return autorizada;
  }

  @override
  Future<bool?> estadoAlarmesExatos() async {
    final android = await _Plugin.android();
    return android?.canScheduleExactNotifications();
  }

  @override
  Future<bool?> pedirAlarmesExatos() async {
    final android = await _Plugin.android();
    return android?.requestExactAlarmsPermission();
  }
}
