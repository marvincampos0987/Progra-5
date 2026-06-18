import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ClienteService } from '../../../services/cliente.service';
import { TipoCedulaService } from '../../../services/tipo-cedula.service';
import { Cliente, TipoCedula } from '../../../models';

@Component({
  selector: 'app-clientes-form',
  templateUrl: './clientes-form.page.html',
  styleUrls: ['./clientes-form.page.scss'],
  standalone: false,
})
export class ClientesFormPage implements OnInit {
  form!: FormGroup;
  esEdicion = false;
  id?: number;
  tiposCedula: TipoCedula[] = [];

  private fb = inject(FormBuilder);
  private clienteService = inject(ClienteService);
  private tipoCedulaService = inject(TipoCedulaService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  ngOnInit() {
    this.form = this.fb.group({
      nombre: ['', [Validators.required, Validators.maxLength(100)]],
      email: ['', [Validators.email]],
      telefono: [''],
      activo: [true],
      tipoCedula: [1, Validators.required],
    });

    this.tipoCedulaService.obtenerTodos().subscribe((data) => {
      if (data) this.tiposCedula = data;
    });

    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam) {
      this.esEdicion = true;
      this.id = +idParam;
      this.clienteService.obtenerPorId(this.id).subscribe((data) => {
        if (data) {
          this.form.patchValue(data);
        }
      });
    }
  }

  guardar() {
    if (this.form.invalid) return;
    const cliente = this.form.value as Cliente;

    if (this.esEdicion && this.id) {
      cliente.clienteId = this.id;
      this.clienteService.actualizar(this.id, cliente).subscribe(() => {
        this.router.navigate(['/clientes']);
      });
    } else {
      this.clienteService.crear(cliente).subscribe(() => {
        this.router.navigate(['/clientes']);
      });
    }
  }
}
