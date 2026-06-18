import { Component, OnInit, inject } from '@angular/core';
import { Router } from '@angular/router';
import { CategoriaService } from '../../services/categoria.service';
import { Categoria } from '../../models';

@Component({
  selector: 'app-categorias',
  templateUrl: './categorias.page.html',
  styleUrls: ['./categorias.page.scss'],
  standalone: false,
})
export class CategoriasPage implements OnInit {
  categorias: Categoria[] = [];
  private categoriaService = inject(CategoriaService);
  private router = inject(Router);

  ngOnInit() {
    this.cargar();
  }

  cargar() {
    this.categoriaService.obtenerTodos().subscribe((data) => {
      if (data) this.categorias = data;
    });
  }

  nuevo() {
    this.router.navigate(['/categorias/nuevo']);
  }

  editar(id: number) {
    this.router.navigate(['/categorias/editar', id]);
  }

  eliminar(id: number) {
    this.categoriaService.eliminar(id).subscribe(() => this.cargar());
  }
}
