import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { catchError, EMPTY, map, Observable } from 'rxjs';
import { environment } from '../../environments/environment.development';
import { TipoCedula } from '../models';

const baseUrl = environment.baseUrl;

@Injectable({ providedIn: 'root' })
export class TipoCedulaService {
  private http = inject(HttpClient);

  obtenerTodos(): Observable<TipoCedula[]> {
    return this.http.get<TipoCedula[]>(`${baseUrl}/TipoCedula`).pipe(
      map((resp) => resp),
      catchError((error: any) => this.handleError(error))
    );
  }

  obtenerPorId(id: number): Observable<TipoCedula | false> {
    return this.http.get<TipoCedula>(`${baseUrl}/TipoCedula/${id}`).pipe(
      map((resp) => resp),
      catchError((error: any) => this.handleError(error))
    );
  }

  crear(tipo: TipoCedula): Observable<TipoCedula | false> {
    return this.http.post<TipoCedula>(`${baseUrl}/TipoCedula`, tipo).pipe(
      map((resp) => resp),
      catchError((error: any) => this.handleError(error))
    );
  }

  actualizar(id: number, tipo: TipoCedula): Observable<any> {
    return this.http.put(`${baseUrl}/TipoCedula/${id}`, tipo).pipe(
      map((resp) => resp),
      catchError((error: any) => this.handleError(error))
    );
  }

  eliminar(id: number): Observable<any> {
    return this.http.delete(`${baseUrl}/TipoCedula/${id}`).pipe(
      map((resp) => resp),
      catchError((error: any) => this.handleError(error))
    );
  }

  private handleError(error: any): typeof EMPTY {
    console.error('Error en TipoCedulaService:', error);
    return EMPTY;
  }
}
