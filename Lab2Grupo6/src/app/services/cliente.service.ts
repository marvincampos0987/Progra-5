import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { catchError, EMPTY, map, Observable } from 'rxjs';
import { environment } from '../../environments/environment.development';
import { Cliente } from '../models';

const baseUrl = environment.baseUrl;

@Injectable({ providedIn: 'root' })
export class ClienteService {
  private http = inject(HttpClient);

  obtenerTodos(): Observable<Cliente[]> {
    return this.http.get<Cliente[]>(`${baseUrl}/Clientes`).pipe(
      map((resp) => resp),
      catchError((error: any) => this.handleError(error))
    );
  }

  obtenerPorId(id: number): Observable<Cliente | false> {
    return this.http.get<Cliente>(`${baseUrl}/Clientes/${id}`).pipe(
      map((resp) => resp),
      catchError((error: any) => this.handleError(error))
    );
  }

  crear(cliente: Cliente): Observable<Cliente | false> {
    return this.http.post<Cliente>(`${baseUrl}/Clientes`, cliente).pipe(
      map((resp) => resp),
      catchError((error: any) => this.handleError(error))
    );
  }

  actualizar(id: number, cliente: Cliente): Observable<any> {
    return this.http.put(`${baseUrl}/Clientes/${id}`, cliente).pipe(
      map((resp) => resp),
      catchError((error: any) => this.handleError(error))
    );
  }

  eliminar(id: number): Observable<any> {
    return this.http.delete(`${baseUrl}/Clientes/${id}`).pipe(
      map((resp) => resp),
      catchError((error: any) => this.handleError(error))
    );
  }

  private handleError(error: any): typeof EMPTY {
    console.error('Error en ClienteService:', error);
    return EMPTY;
  }
}
