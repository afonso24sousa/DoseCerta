import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiConfig } from '../core/api-config';
import { DocumentoDto, DocumentoSubmetido } from '../models/fichas.models';

/**
 * Operações dos documentos das fichas (F002): OP-15 a OP-17. Os ficheiros não têm endereço público: o conteúdo só se
 * obtém por OP-17, com a sessão (RNF-007).
 */
@Injectable({ providedIn: 'root' })
export class DocumentosApi {
  private readonly http = inject(HttpClient);
  private readonly config = inject(ApiConfig);

  /** OP-15: carrega um documento na ficha, com as partes `categoria` e `ficheiro` (RF-022, RNF-008). */
  carregar(fichaId: string, documento: DocumentoSubmetido): Observable<DocumentoDto> {
    const formulario = new FormData();
    formulario.append('categoria', documento.categoria);
    formulario.append('ficheiro', documento.ficheiro, documento.ficheiro.name);
    return this.http.post<DocumentoDto>(
      this.config.url(`/fichas/${encodeURIComponent(fichaId)}/documentos`),
      formulario,
    );
  }

  /** OP-16: metadados e categoria dos documentos da ficha (RF-022, RF-028). */
  listar(fichaId: string): Observable<DocumentoDto[]> {
    return this.http.get<DocumentoDto[]>(
      this.config.url(`/fichas/${encodeURIComponent(fichaId)}/documentos`),
    );
  }

  /** OP-17: conteúdo do documento, para abrir ou guardar no navegador (RF-028, RF-124). */
  descarregar(documentoId: string): Observable<Blob> {
    return this.http.get(this.config.url(`/documentos/${encodeURIComponent(documentoId)}`), {
      responseType: 'blob',
    });
  }
}
