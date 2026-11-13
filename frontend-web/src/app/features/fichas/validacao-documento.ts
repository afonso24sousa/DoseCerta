/** Tamanho máximo de um documento, como no servidor: 10 MB (RNF-008). */
export const TAMANHO_MAXIMO_DOCUMENTO = 10 * 1024 * 1024;

/** Valor do atributo `accept` dos campos de ficheiro: PDF, JPG e PNG (RNF-008). */
export const ACEITAR_DOCUMENTOS = '.pdf,.jpg,.jpeg,.png,application/pdf,image/jpeg,image/png';

/** Extensões aceites para cada tipo de conteúdo. */
const EXTENSOES_POR_TIPO: Readonly<Record<string, readonly string[]>> = {
  'application/pdf': ['.pdf'],
  'image/jpeg': ['.jpg', '.jpeg'],
  'image/png': ['.png'],
};

const EXTENSOES_ACEITES = Object.values(EXTENSOES_POR_TIPO).flat();

/**
 * Verificação prévia de um ficheiro escolhido, antes de o enviar (RNF-008): só PDF, JPG e PNG, pela extensão e pelo tipo
 * indicado pelo navegador (quando o indica, tem de corresponder à extensão), não vazio e com 10 MB no máximo. O
 * servidor confirma sempre o formato pelo conteúdo do ficheiro.
 *
 * @returns A regra violada, ou `null` se o ficheiro pode ser enviado.
 */
export function validarDocumento(ficheiro: File): string | null {
  const nome = ficheiro.name.toLowerCase();
  const extensao = nome.includes('.') ? nome.slice(nome.lastIndexOf('.')) : '';
  const tipo = ficheiro.type.toLowerCase();
  const formatoValido =
    EXTENSOES_ACEITES.includes(extensao) &&
    (tipo === '' || (EXTENSOES_POR_TIPO[tipo]?.includes(extensao) ?? false));

  if (!formatoValido) {
    return `O ficheiro «${ficheiro.name}» não é PDF, JPG nem PNG; só estes formatos são aceites.`;
  }

  if (ficheiro.size === 0) {
    return `O ficheiro «${ficheiro.name}» está vazio.`;
  }

  if (ficheiro.size > TAMANHO_MAXIMO_DOCUMENTO) {
    return `O ficheiro «${ficheiro.name}» tem mais de 10 MB; o tamanho máximo é 10 MB.`;
  }

  return null;
}
