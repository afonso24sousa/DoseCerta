# DoseCerta: frontend web

Aplicação Angular 18 (componentes standalone e signals) do DoseCerta para o navegador. Fala com a API REST do backend
(`docs/api/`) através do endereço configurado em `src/environments`.

## Requisitos

- Node.js 20 LTS e npm (as versões das dependências estão fixadas em `package.json` e `package-lock.json`).

## Instalar

```bash
npm ci
```

## Correr em desenvolvimento

O backend não tem CORS: em desenvolvimento os pedidos a `/api` passam pelo proxy do `ng serve`, na mesma origem
HTTPS (`https://localhost:4200`, com o certificado de desenvolvimento do Angular, RNF-002).

```bash
npm start            # proxy para o backend publicado (proxy.conf.json)
npm run start:local  # proxy para o backend local em https://localhost:7180 (proxy.local.conf.json)
```

Abrir `https://localhost:4200` e aceitar o certificado de desenvolvimento.

## Construir, verificar e formatar

```bash
npm run build         # build de produção em dist/dosecerta-web
npm run lint          # ESLint 9 + angular-eslint 18 (eslint.config.js)
npm run format:check  # Prettier 3 (.prettierrc), sem alterar ficheiros
npm run format        # aplica a formatação do Prettier
```

## Estrutura

| Pasta              | Conteúdo                                                                                                |
| ------------------ | ------------------------------------------------------------------------------------------------------- |
| `src/app/core`     | Configuração da API, sessão (`AuthService`, `TokenStorage`), interceptores, guardas e mensagens de erro |
| `src/app/models`   | DTO da API e `ProblemaApi`                                                                              |
| `src/app/api`      | Uma classe por grupo de operações (`AuthApi`, `ContaApi`, `FichasApi`, `DocumentosApi`, `DominiosApi`)  |
| `src/app/shared`   | Validadores e componentes reutilizáveis dos formulários                                                 |
| `src/app/layout`   | Estrutura das páginas autenticadas (cabeçalho e barra lateral)                                          |
| `src/app/features` | Páginas, por funcionalidade                                                                             |
