import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiConfig } from '../core/api-config';
import {
  DadosFichaDto,
  DocumentoSubmetido,
  EstadoFicha,
  FichaDto,
  FichaResumoDto,
  NovaFichaDto,
} from '../models/fichas.models';

/**
 * Operações das fichas de utentes (F002 e F003): OP-07 e OP-09 a OP-14. A criação e a edição usam `application/json`
 * sem documentos e `multipart/form-data` com documentos (documentação da API, secção 3.6).
 */
@Injectable({ providedIn: 'root' })
export class FichasApi {
  private readonly http = inject(HttpClient);
  private readonly config = inject(ApiConfig);

  /** OP-07: fichas a que o Utilizador está associado; sem `estado`, as ativas e as arquivadas (RF-028, RF-121). */
  listar(estado?: EstadoFicha): Observable<FichaResumoDto[]> {
    const params = estado ? new HttpParams().set('estado', estado) : undefined;
    return this.http.get<FichaResumoDto[]>(this.config.url('/fichas'), { params });
  }

  /** OP-09: cria a ficha própria ou de terceiro, com os documentos opcionais (RF-020 a RF-025, RF-030). */
  criar(dados: NovaFichaDto, documentos: readonly DocumentoSubmetido[]): Observable<FichaDto> {
    return this.http.post<FichaDto>(this.config.url('/fichas'), FichasApi.corpo(dados, documentos));
  }

  /** OP-10: dados de identificação e necessidades da ficha (RF-028). */
  consultar(fichaId: string): Observable<FichaDto> {
    return this.http.get<FichaDto>(this.config.url(`/fichas/${encodeURIComponent(fichaId)}`));
  }

  /** OP-11: altera os dados da ficha e acrescenta os documentos opcionais (RF-029). */
  editar(
    fichaId: string,
    dados: DadosFichaDto,
    documentos: readonly DocumentoSubmetido[],
  ): Observable<FichaDto> {
    return this.http.put<FichaDto>(
      this.config.url(`/fichas/${encodeURIComponent(fichaId)}`),
      FichasApi.corpo(dados, documentos),
    );
  }

  /** OP-12: arquiva a ficha, que passa a só de consulta (RF-120). */
  arquivar(fichaId: string): Observable<void> {
    return this.http.post<void>(this.config.url(`/fichas/${encodeURIComponent(fichaId)}/arquivo`), null);
  }

  /** OP-13: reativa a ficha arquivada (RF-121). */
  reativar(fichaId: string): Observable<void> {
    return this.http.delete<void>(this.config.url(`/fichas/${encodeURIComponent(fichaId)}/arquivo`));
  }

  /** OP-14: elimina a ficha sem medicação nem histórico (RF-123). */
  eliminar(fichaId: string): Observable<void> {
    return this.http.delete<void>(this.config.url(`/fichas/${encodeURIComponent(fichaId)}`));
  }

  /**
   * Corpo de OP-09 e OP-11: o próprio objeto (`application/json`) sem documentos; com documentos, um `FormData` com a
   * parte `dados` (JSON) e, por documento e por esta ordem, as partes `documento` e `categoria`.
   */
  private static corpo(
    dados: DadosFichaDto,
    documentos: readonly DocumentoSubmetido[],
  ): DadosFichaDto | FormData {
    if (documentos.length === 0) {
      return dados;
    }

    const formulario = new FormData();
    formulario.append('dados', JSON.stringify(dados));
    for (const documento of documentos) {
      formulario.append('documento', documento.ficheiro, documento.ficheiro.name);
      formulario.append('categoria', documento.categoria);
    }

    return formulario;
  }
}
