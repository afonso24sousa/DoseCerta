# DoseCerta: frontend mobile

Aplicação Flutter 3 (Dart 3), só Android (RNF-056), que consome a API do DoseCerta. A organização das pastas em `lib/` segue o modelo de classes do frontend mobile (`m3-modelo-classes-frontend-mobile-v02.md`, secção 3): `core`, `models`, `data`, `shared` e `features`.

## Endereço da API

O endereço da API é a raiz do servidor (sem `/api/v1`) e é indicado no momento da compilação com a variável `URL_API`:

```powershell
flutter run --dart-define=URL_API=https://dosecerta.spaincentral.cloudapp.azure.com
```

Sem `URL_API`, a aplicação usa o ambiente de demonstração público acima. Só são aceites endereços `https://` (RNF-002). A única exceção é `http://10.0.2.2` (a API local vista a partir do emulador Android) e só em builds debug, para os testes de sistema; ver `android/app/src/debug/res/xml/network_security_config.xml`.

## Verificações (as mesmas da frontend-mobile-ci)

```powershell
flutter pub get
dart format --output=none --set-exit-if-changed .
flutter analyze --fatal-warnings
flutter build apk --debug
```
