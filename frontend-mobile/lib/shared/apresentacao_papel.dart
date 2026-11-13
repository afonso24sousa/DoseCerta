import '../models/papel.dart';

/// Texto do papel do Utilizador numa ficha, como no protótipo (E05 e E08): «Cuidador principal», «Cuidador» ou
/// «Familiar». O Cuidador principal é um Cuidador com `souPrincipal` (matriz de permissões, secção 3).
String rotuloPapel(Papel papel, {required bool principal}) {
  if (papel == Papel.cuidador && principal) {
    return 'Cuidador principal';
  }
  return papel.valor;
}
