import { Component, OnInit, inject } from '@angular/core';

import { FormBuilder, FormGroup, Validators } from '@angular/forms';

import { ActivatedRoute, Router } from '@angular/router';

import { DetallesPedidoService } from '../../../services/detalles-pedido.service';

import { PedidoService } from '../../../services/pedido.service';

import { ProductoService } from '../../../services/producto.service';

import { DetallesPedido, Pedido, Producto } from '../../../models';



@Component({

  selector: 'app-detalles-pedido-form',

  templateUrl: './detalles-pedido-form.page.html',

  styleUrls: ['./detalles-pedido-form.page.scss'],

  standalone: false,

})

export class DetallesPedidoFormPage implements OnInit {

  form!: FormGroup;

  esEdicion = false;

  id?: number;

  pedidos: Pedido[] = [];

  productos: Producto[] = [];



  private fb = inject(FormBuilder);

  private detallesPedidoService = inject(DetallesPedidoService);

  private pedidoService = inject(PedidoService);

  private productoService = inject(ProductoService);

  private route = inject(ActivatedRoute);

  private router = inject(Router);



  ngOnInit() {

    this.form = this.fb.group({

      pedidoId: [null, Validators.required],

      productoId: ['', Validators.required],

      cantidad: [1, Validators.required],

      precioUnitario: [0, Validators.required],

      descuento: [0],

    });



    this.pedidoService.obtenerTodos().subscribe((data) => {

      if (data) this.pedidos = data;

    });



    this.productoService.obtenerTodos().subscribe((data) => {

      if (data) this.productos = data;

    });



    const idParam = this.route.snapshot.paramMap.get('id');

    if (idParam) {

      this.esEdicion = true;

      this.id = +idParam;

      this.detallesPedidoService.obtenerPorId(this.id).subscribe((data) => {

        if (data) {

          this.form.patchValue(data);

        }

      });

    }

  }



  guardar() {

    if (this.form.invalid) return;

    const detalle = this.form.value as DetallesPedido;



    if (this.esEdicion && this.id) {

      detalle.detalleId = this.id;

      this.detallesPedidoService.actualizar(this.id, detalle).subscribe(() => {

        this.router.navigate(['/detalles-pedido']);

      });

    } else {

      this.detallesPedidoService.crear(detalle).subscribe(() => {

        this.router.navigate(['/detalles-pedido']);

      });

    }

  }

}


