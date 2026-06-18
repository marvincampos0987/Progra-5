import { Component, OnInit, inject } from '@angular/core';
import { Router } from '@angular/router';
import { TipoCedulaService } from '../../services/tipo-cedula.service';
import { TipoCedula } from '../../models';

@Component({
  selector: 'app-tipo-cedula',
  templateUrl: './tipo-cedula.page.html',
  styleUrls: ['./tipo-cedula.page.scss'],
  standalone: false,
})
export class TipoCedulaPage implements OnInit {
  tiposCedula: TipoCedula[] = [];
  private tipoCedulaService = inject(TipoCedulaService);
  private router = inject(Router);

  ngOnInit() {
    this.cargar();
  }

  cargar() {
    this.tipoCedulaService.obtenerTodos().subscribe((data) => {
      if (data) this.tiposCedula = data;
    });
  }

  nuevo() {
    this.router.navigate(['/tipo-cedula/nuevo']);
  }

  editar(id: number) {
    this.router.navigate(['/tipo-cedula/editar', id]);
  }

  eliminar(id: number) {
    this.tipoCedulaService.eliminar(id).subscribe(() => this.cargar());
  }
}
