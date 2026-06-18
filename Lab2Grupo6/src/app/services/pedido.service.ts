import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { catchError, EMPTY, map, Observable } from 'rxjs';
import { environment } from '../../environments/environment.development';
import { Pedido } from '../models';

const baseUrl = environment.baseUrl;

@Injectable({ providedIn: 'root' })
export class PedidoService {
  private http = inject(HttpClient);

  obtenerTodos(): Observable<Pedido[]> {
    return this.http.get<Pedido[]>(`${baseUrl}/Pedidos`).pipe(
      map((resp) => resp),
      catchError((error: any) => this.handleError(error))
    );
  }

  obtenerPorId(id: number): Observable<Pedido | false> {
    return this.http.get<Pedido>(`${baseUrl}/Pedidos/${id}`).pipe(
      map((resp) => resp),
      catchError((error: any) => this.handleError(error))
    );
  }

  crear(pedido: Pedido): Observable<Pedido | false> {
    return this.http.post<Pedido>(`${baseUrl}/Pedidos`, pedido).pipe(
      map((resp) => resp),
      catchError((error: any) => this.handleError(error))
    );
  }

  actualizar(id: number, pedido: Pedido): Observable<any> {
    return this.http.put(`${baseUrl}/Pedidos/${id}`, pedido).pipe(
      map((resp) => resp),
      catchError((error: any) => this.handleError(error))
    );
  }

  eliminar(id: number): Observable<any> {
    return this.http.delete(`${baseUrl}/Pedidos/${id}`).pipe(
      map((resp) => resp),
      catchError((error: any) => this.handleError(error))
    );
  }

  private handleError(error: any): typeof EMPTY {
    console.error('Error en PedidoService:', error);
    return EMPTY;
  }
}
