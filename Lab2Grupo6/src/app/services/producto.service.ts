import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { catchError, EMPTY, map, Observable } from 'rxjs';
import { environment } from '../../environments/environment.development';
import { Producto } from '../models';

const baseUrl = environment.baseUrl;

@Injectable({ providedIn: 'root' })
export class ProductoService {
  private http = inject(HttpClient);

  obtenerTodos(): Observable<Producto[]> {
    return this.http.get<Producto[]>(`${baseUrl}/Productos`).pipe(
      map((resp) => resp),
      catchError((error: any) => this.handleError(error))
    );
  }

  obtenerPorId(id: string): Observable<Producto | false> {
    return this.http.get<Producto>(`${baseUrl}/Productos/${id}`).pipe(
      map((resp) => resp),
      catchError((error: any) => this.handleError(error))
    );
  }

  crear(producto: Producto): Observable<Producto | false> {
    return this.http.post<Producto>(`${baseUrl}/Productos`, producto).pipe(
      map((resp) => resp),
      catchError((error: any) => this.handleError(error))
    );
  }

  actualizar(id: string, producto: Producto): Observable<any> {
    return this.http.put(`${baseUrl}/Productos/${id}`, producto).pipe(
      map((resp) => resp),
      catchError((error: any) => this.handleError(error))
    );
  }

  eliminar(id: string): Observable<any> {
    return this.http.delete(`${baseUrl}/Productos/${id}`).pipe(
      map((resp) => resp),
      catchError((error: any) => this.handleError(error))
    );
  }

  private handleError(error: any): typeof EMPTY {
    console.error('Error en ProductoService:', error);
    return EMPTY;
  }
}
