import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { catchError, EMPTY, map, Observable } from 'rxjs';
import { environment } from '../../environments/environment.development';
import { Categoria } from '../models';

const baseUrl = environment.baseUrl;

@Injectable({ providedIn: 'root' })
export class CategoriaService {
  private http = inject(HttpClient);

  obtenerTodos(): Observable<Categoria[]> {
    return this.http.get<Categoria[]>(`${baseUrl}/Categorias`).pipe(
      map((resp) => resp),
      catchError((error: any) => this.handleError(error))
    );
  }

  obtenerPorId(id: number): Observable<Categoria | false> {
    return this.http.get<Categoria>(`${baseUrl}/Categorias/${id}`).pipe(
      map((resp) => resp),
      catchError((error: any) => this.handleError(error))
    );
  }

  crear(categoria: Categoria): Observable<Categoria | false> {
    return this.http.post<Categoria>(`${baseUrl}/Categorias`, categoria).pipe(
      map((resp) => resp),
      catchError((error: any) => this.handleError(error))
    );
  }

  actualizar(id: number, categoria: Categoria): Observable<any> {
    return this.http.put(`${baseUrl}/Categorias/${id}`, categoria).pipe(
      map((resp) => resp),
      catchError((error: any) => this.handleError(error))
    );
  }

  eliminar(id: number): Observable<any> {
    return this.http.delete(`${baseUrl}/Categorias/${id}`).pipe(
      map((resp) => resp),
      catchError((error: any) => this.handleError(error))
    );
  }

  private handleError(error: any): typeof EMPTY {
    console.error('Error en CategoriaService:', error);
    return EMPTY;
  }
}
