import { Component, OnInit, inject } from '@angular/core';
import { Router } from '@angular/router';
import { DetallesPedidoService } from '../../services/detalles-pedido.service';
import { DetallesPedido } from '../../models';

@Component({
  selector: 'app-detalles-pedido',
  templateUrl: './detalles-pedido.page.html',
  styleUrls: ['./detalles-pedido.page.scss'],
  standalone: false,
})
export class DetallesPedidoPage implements OnInit {
  detalles: DetallesPedido[] = [];
  private detallesPedidoService = inject(DetallesPedidoService);
  private router = inject(Router);

  ngOnInit() {
    this.cargar();
  }

  cargar() {
    this.detallesPedidoService.obtenerTodos().subscribe((data) => {
      if (data) this.detalles = data;
    });
  }

  nuevo() {
    this.router.navigate(['/detalles-pedido/nuevo']);
  }

  editar(id: number) {
    this.router.navigate(['/detalles-pedido/editar', id]);
  }

  eliminar(id: number) {
    this.detallesPedidoService.eliminar(id).subscribe(() => this.cargar());
  }
}
