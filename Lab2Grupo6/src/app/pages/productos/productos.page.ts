import { Component, OnInit, inject } from '@angular/core';
import { Router } from '@angular/router';
import { ProductoService } from '../../services/producto.service';
import { Producto } from '../../models';

@Component({
  selector: 'app-productos',
  templateUrl: './productos.page.html',
  styleUrls: ['./productos.page.scss'],
  standalone: false,
})
export class ProductosPage implements OnInit {
  productos: Producto[] = [];
  private productoService = inject(ProductoService);
  private router = inject(Router);

  ngOnInit() {
    this.cargar();
  }

  cargar() {
    this.productoService.obtenerTodos().subscribe((data) => {
      if (data) this.productos = data;
    });
  }

  nuevo() {
    this.router.navigate(['/productos/nuevo']);
  }

  editar(id: string) {
    this.router.navigate(['/productos/editar', id]);
  }

  eliminar(id: string) {
    this.productoService.eliminar(id).subscribe(() => this.cargar());
  }
}
