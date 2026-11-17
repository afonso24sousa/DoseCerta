import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/provedores.dart';
import '../../models/tratamento_dto.dart';

/// Tratamentos da ficha para E15 (OP-28, sem `estado`: os em curso e os terminados; RF-054, RF-057), por ordem
/// do medicamento. Depois de registar, editar ou terminar, os ecrãs invalidam este fornecedor para a lista voltar a
/// ser pedida.
final tratamentosFichaProvider = FutureProvider.autoDispose
    .family<List<TratamentoDto>, String>((ref, fichaId) async {
      final tratamentos = await ref
          .read(tratamentosRepositorioProvider)
          .listar(fichaId);
      return [...tratamentos]..sort(
        (a, b) =>
            a.medicamento.toLowerCase().compareTo(b.medicamento.toLowerCase()),
      );
    });
