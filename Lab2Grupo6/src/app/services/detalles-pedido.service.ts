import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { catchError, EMPTY, map, Observable } from 'rxjs';
import { environment } from '../../environments/environment.development';
import { DetallesPedido } from '../models';

const baseUrl = environment.baseUrl;

@Injectable({ providedIn: 'root' })
export class DetallesPedidoService {
  private http = inject(HttpClient);

  obtenerTodos(): Observable<DetallesPedido[]> {
    return this.http.get<DetallesPedido[]>(`${baseUrl}/DetallesPedido`).pipe(
      map((resp) => resp),
      catchError((error: any) => this.handleError(error))
    );
  }

  obtenerPorId(id: number): Observable<DetallesPedido | false> {
    return this.http.get<DetallesPedido>(`${baseUrl}/DetallesPedido/${id}`).pipe(
      map((resp) => resp),
      catchError((error: any) => this.handleError(error))
    );
  }

  crear(detalle: DetallesPedido): Observable<DetallesPedido | false> {
    return this.http.post<DetallesPedido>(`${baseUrl}/DetallesPedido`, detalle).pipe(
      map((resp) => resp),
      catchError((error: any) => this.handleError(error))
    );
  }

  actualizar(id: number, detalle: DetallesPedido): Observable<any> {
    return this.http.put(`${baseUrl}/DetallesPedido/${id}`, detalle).pipe(
      map((resp) => resp),
      catchError((error: any) => this.handleError(error))
    );
  }

  eliminar(id: number): Observable<any> {
    return this.http.delete(`${baseUrl}/DetallesPedido/${id}`).pipe(
      map((resp) => resp),
      catchError((error: any) => this.handleError(error))
    );
  }

  private handleError(error: any): typeof EMPTY {
    console.error('Error en DetallesPedidoService:', error);
    return EMPTY;
  }
}
