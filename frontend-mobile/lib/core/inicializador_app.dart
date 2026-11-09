import 'package:flutter/widgets.dart';
import 'package:flutter_riverpod/misc.dart' show Override;
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

import 'configuracao_api.dart';
import 'provedores.dart';
import 'sessao_store.dart';

/// Prepara o arranque a frio: valida o endereço da API e restaura a sessão só a partir do armazenamento local,
/// sem pedidos de rede, para o primeiro ecrã abrir depressa (RNF-016).
class InicializadorApp {
  /// Cria o inicializador.
  const InicializadorApp({
    this.armazenamento = const SessaoStore(FlutterSecureStorage()),
  });

  /// Armazenamento da sessão.
  final SessaoStore armazenamento;

  /// Devolve as substituições dos fornecedores com a configuração e a sessão lidas no arranque.
  Future<List<Override>> iniciar() async {
    WidgetsFlutterBinding.ensureInitialized();
    final configuracao = ConfiguracaoApi.doAmbiente();
    SessaoGuardada? sessao;
    try {
      sessao = await armazenamento.ler();
    } on Exception {
      // Armazenamento ilegível (por exemplo, depois de restaurar uma cópia de segurança): começa sem sessão.
      await armazenamento.limpar();
    }
    return [
      configuracaoApiProvider.overrideWithValue(configuracao),
      sessaoStoreProvider.overrideWithValue(armazenamento),
      sessaoInicialProvider.overrideWithValue(sessao),
    ];
  }
}
