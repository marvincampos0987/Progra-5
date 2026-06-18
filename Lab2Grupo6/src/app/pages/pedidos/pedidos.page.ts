import { Component, OnInit, inject } from '@angular/core';
import { Router } from '@angular/router';
import { PedidoService } from '../../services/pedido.service';
import { Pedido } from '../../models';

@Component({
  selector: 'app-pedidos',
  templateUrl: './pedidos.page.html',
  styleUrls: ['./pedidos.page.scss'],
  standalone: false,
})
export class PedidosPage implements OnInit {
  pedidos: Pedido[] = [];
  private pedidoService = inject(PedidoService);
  private router = inject(Router);

  ngOnInit() {
    this.cargar();
  }

  cargar() {
    this.pedidoService.obtenerTodos().subscribe((data) => {
      if (data) this.pedidos = data;
    });
  }

  nuevo() {
    this.router.navigate(['/pedidos/nuevo']);
  }

  editar(id: number) {
    this.router.navigate(['/pedidos/editar', id]);
  }

  eliminar(id: number) {
    this.pedidoService.eliminar(id).subscribe(() => this.cargar());
  }
}
