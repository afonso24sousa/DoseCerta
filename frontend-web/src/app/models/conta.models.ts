/** Pedido de OP-01 Registar conta (RF-010). */
export interface NovaContaDto {
  nome: string;
  email: string;
  palavraPasse: string;
}

/** Resposta de OP-01: a conta criada e o número de convites pendentes para o e-mail (RF-044). */
export interface ContaCriadaDto {
  id: string;
  nome: string;
  email: string;
  convitesPendentes: number;
}

/** Pedido de OP-02 Iniciar sessão (RF-012). */
export interface CredenciaisDto {
  email: string;
  palavraPasse: string;
}

/** Resposta de OP-02: o token JWT, o tipo e o instante de expiração, com o desvio de Lisboa (RNF-009, RNF-054). */
export interface SessaoDto {
  token: string;
  tipo: string;
  expiraEm: string;
}

/** Resposta de OP-04 e OP-05: os dados da conta, sem a palavra-passe (RF-013). */
export interface UtilizadorDto {
  id: string;
  nome: string;
  email: string;
}

/** Pedido de OP-05 Atualizar dados da conta (RF-014). */
export interface DadosContaDto {
  nome: string;
  email: string;
}

/** Pedido de OP-06 Alterar palavra-passe (RF-015). */
export interface AlterarPalavraPasseDto {
  palavraPasseAtual: string;
  palavraPasseNova: string;
}
