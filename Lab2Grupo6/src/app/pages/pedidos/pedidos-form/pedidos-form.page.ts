import { Component, OnInit, inject } from '@angular/core';

import { FormBuilder, FormGroup, Validators } from '@angular/forms';

import { ActivatedRoute, Router } from '@angular/router';

import { PedidoService } from '../../../services/pedido.service';

import { ClienteService } from '../../../services/cliente.service';

import { Cliente, Pedido } from '../../../models';



@Component({

  selector: 'app-pedidos-form',

  templateUrl: './pedidos-form.page.html',

  styleUrls: ['./pedidos-form.page.scss'],

  standalone: false,

})

export class PedidosFormPage implements OnInit {

  form!: FormGroup;

  esEdicion = false;

  id?: number;

  clientes: Cliente[] = [];



  private fb = inject(FormBuilder);

  private pedidoService = inject(PedidoService);

  private clienteService = inject(ClienteService);

  private route = inject(ActivatedRoute);

  private router = inject(Router);



  ngOnInit() {

    this.form = this.fb.group({

      clienteId: [null, Validators.required],

      total: [null],

      moneda: ['', Validators.required],

    });



    this.clienteService.obtenerTodos().subscribe((data) => {

      if (data) this.clientes = data;

    });



    const idParam = this.route.snapshot.paramMap.get('id');

    if (idParam) {

      this.esEdicion = true;

      this.id = +idParam;

      this.pedidoService.obtenerPorId(this.id).subscribe((data) => {

        if (data) {

          this.form.patchValue(data);

        }

      });

    }

  }



  guardar() {

    if (this.form.invalid) return;

    const pedido = this.form.value as Pedido;



    if (this.esEdicion && this.id) {

      pedido.pedidoId = this.id;

      this.pedidoService.actualizar(this.id, pedido).subscribe(() => {

        this.router.navigate(['/pedidos']);

      });

    } else {

      this.pedidoService.crear(pedido).subscribe(() => {

        this.router.navigate(['/pedidos']);

      });

    }

  }

}


