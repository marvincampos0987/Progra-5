import { Component, OnInit, inject } from '@angular/core';
import { Router } from '@angular/router';
import { ClienteService } from '../../services/cliente.service';
import { Cliente } from '../../models';

@Component({
  selector: 'app-clientes',
  templateUrl: './clientes.page.html',
  styleUrls: ['./clientes.page.scss'],
  standalone: false,
})
export class ClientesPage implements OnInit {
  clientes: Cliente[] = [];
  private clienteService = inject(ClienteService);
  private router = inject(Router);

  ngOnInit() {
    this.cargar();
  }

  cargar() {
    this.clienteService.obtenerTodos().subscribe((data) => {
      if (data) this.clientes = data;
    });
  }

  nuevo() {
    this.router.navigate(['/clientes/nuevo']);
  }

  editar(id: number) {
    this.router.navigate(['/clientes/editar', id]);
  }

  eliminar(id: number) {
    this.clienteService.eliminar(id).subscribe(() => this.cargar());
  }
}
