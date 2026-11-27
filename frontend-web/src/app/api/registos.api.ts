import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiConfig } from '../core/api-config';
import { NovaNotaDto, NovaOcorrenciaDto, RegistoDto, TipoRegisto } from '../models/registos.models';

/** Ocorrências diárias e notas partilhadas de uma ficha (F009): OP-46 a OP-49. */
@Injectable({ providedIn: 'root' })
export class RegistosApi {
  private readonly http = inject(HttpClient);
  private readonly config = inject(ApiConfig);

  /**
   * OP-48 (`Ocorrencia`) e OP-49 (`Nota`): registos da ficha, do mais recente para o mais antigo pelo momento do
   * registo. As notas só são devolvidas aos Cuidadores; o Familiar recebe `403` (RF-095).
   */
  listar(fichaId: string, tipo: TipoRegisto): Observable<RegistoDto[]> {
    const params = new HttpParams().set('tipo', tipo);
    return this.http.get<RegistoDto[]>(this.config.url(`/fichas/${encodeURIComponent(fichaId)}/registos`), {
      params,
    });
  }

  /** OP-46: regista uma ocorrência; os outros Cuidadores e os Familiares da ficha são notificados (RF-115). */
  registarOcorrencia(fichaId: string, dados: NovaOcorrenciaDto): Observable<RegistoDto> {
    return this.http.post<RegistoDto>(
      this.config.url(`/fichas/${encodeURIComponent(fichaId)}/ocorrencias`),
      dados,
    );
  }

  /** OP-47: regista uma nota para os Cuidadores; não gera notificações (RF-115). */
  registarNota(fichaId: string, dados: NovaNotaDto): Observable<RegistoDto> {
    return this.http.post<RegistoDto>(this.config.url(`/fichas/${encodeURIComponent(fichaId)}/notas`), dados);
  }
}
