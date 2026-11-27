import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiConfig } from '../core/api-config';
import { NotificacaoDto } from '../models/notificacoes.models';

/** Histórico de notificações (F011): OP-51. */
@Injectable({ providedIn: 'root' })
export class NotificacoesApi {
  private readonly http = inject(HttpClient);
  private readonly config = inject(ApiConfig);

  /**
   * OP-51: notificações recebidas pelo Utilizador, da mais recente para a mais antiga (RF-140). `de` e `ate` são
   * opcionais (datas `AAAA-MM-DD` em hora de Lisboa, aplicadas ao momento da notificação).
   */
  listar(de?: string, ate?: string): Observable<NotificacaoDto[]> {
    let params = new HttpParams();
    if (de) {
      params = params.set('de', de);
    }
    if (ate) {
      params = params.set('ate', ate);
    }
    return this.http.get<NotificacaoDto[]>(this.config.url('/notificacoes'), { params });
  }
}
