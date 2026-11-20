import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiConfig } from '../core/api-config';
import { DadosTratamentoDto, EstadoTratamento, TratamentoDto } from '../models/tratamentos.models';

/**
 * Tratamentos de uma ficha (F005): OP-27 a OP-31. O registo e a atualização usam `application/json` sem receita e
 * `multipart/form-data` com receita, com as partes `dados` (o JSON do pedido) e `receita` (documentação da API, secção
 * 3.6), como as fichas da I059.
 */
@Injectable({ providedIn: 'root' })
export class TratamentosApi {
  private readonly http = inject(HttpClient);
  private readonly config = inject(ApiConfig);

  /** OP-27: regista o tratamento e a receita opcional; o servidor gera as tomas (RF-050 a RF-053, RF-060). */
  registar(fichaId: string, dados: DadosTratamentoDto, receita?: File | null): Observable<TratamentoDto> {
    return this.http.post<TratamentoDto>(
      this.config.url(`/fichas/${encodeURIComponent(fichaId)}/tratamentos`),
      TratamentosApi.corpo(dados, receita),
    );
  }

  /** OP-28: tratamentos da ficha; sem `estado`, os em curso e os terminados (RF-054, RF-057). */
  listar(fichaId: string, estado?: EstadoTratamento): Observable<TratamentoDto[]> {
    const params = estado ? new HttpParams().set('estado', estado) : undefined;
    return this.http.get<TratamentoDto[]>(
      this.config.url(`/fichas/${encodeURIComponent(fichaId)}/tratamentos`),
      { params },
    );
  }

  /** OP-29: um tratamento, também depois de terminado (RF-054, RF-057). */
  obter(tratamentoId: string): Observable<TratamentoDto> {
    return this.http.get<TratamentoDto>(this.config.url(`/tratamentos/${encodeURIComponent(tratamentoId)}`));
  }

  /** OP-30: altera só os campos enviados de um tratamento em curso, e a receita nova opcional (RF-055). */
  atualizar(
    tratamentoId: string,
    dados: Partial<DadosTratamentoDto>,
    receita?: File | null,
  ): Observable<TratamentoDto> {
    return this.http.put<TratamentoDto>(
      this.config.url(`/tratamentos/${encodeURIComponent(tratamentoId)}`),
      TratamentosApi.corpo(dados, receita),
    );
  }

  /** OP-31: termina o tratamento; as tomas futuras deixam de existir e o histórico mantém-se (RF-056, RF-057). */
  terminar(tratamentoId: string): Observable<TratamentoDto> {
    return this.http.post<TratamentoDto>(
      this.config.url(`/tratamentos/${encodeURIComponent(tratamentoId)}/termino`),
      null,
    );
  }

  /** Corpo de OP-27 e OP-30: o próprio objeto sem receita; com receita, as partes `dados` e `receita`. */
  private static corpo(
    dados: Partial<DadosTratamentoDto>,
    receita?: File | null,
  ): Partial<DadosTratamentoDto> | FormData {
    if (!receita) {
      return dados;
    }

    const formulario = new FormData();
    formulario.append('dados', JSON.stringify(dados));
    formulario.append('receita', receita, receita.name);
    return formulario;
  }
}
