import { Component, OnInit, inject } from '@angular/core';

import { FormBuilder, FormGroup, Validators } from '@angular/forms';

import { ActivatedRoute, Router } from '@angular/router';

import { ProductoService } from '../../../services/producto.service';

import { CategoriaService } from '../../../services/categoria.service';

import { Categoria, Producto } from '../../../models';



@Component({

  selector: 'app-productos-form',

  templateUrl: './productos-form.page.html',

  styleUrls: ['./productos-form.page.scss'],

  standalone: false,

})

export class ProductosFormPage implements OnInit {

  form!: FormGroup;

  esEdicion = false;

  id?: string;

  categorias: Categoria[] = [];



  private fb = inject(FormBuilder);

  private productoService = inject(ProductoService);

  private categoriaService = inject(CategoriaService);

  private route = inject(ActivatedRoute);

  private router = inject(Router);



  ngOnInit() {

    this.form = this.fb.group({

      productoId: ['', Validators.required],

      nombre: ['', Validators.required],

      precio: [0, Validators.required],

      stock: [0, Validators.required],

      categoriaId: [null, Validators.required],

      activo: [true],

    });



    this.categoriaService.obtenerTodos().subscribe((data) => {

      if (data) this.categorias = data;

    });



    const idParam = this.route.snapshot.paramMap.get('id');

    if (idParam) {

      this.esEdicion = true;

      this.id = idParam;

      this.form.get('productoId')?.disable();

      this.productoService.obtenerPorId(this.id).subscribe((data) => {

        if (data) {

          this.form.patchValue(data);

        }

      });

    }

  }



  guardar() {

    if (this.form.invalid) return;

    const producto = this.form.getRawValue() as Producto;



    if (this.esEdicion && this.id) {

      producto.productoId = this.id;

      this.productoService.actualizar(this.id, producto).subscribe(() => {

        this.router.navigate(['/productos']);

      });

    } else {

      this.productoService.crear(producto).subscribe(() => {

        this.router.navigate(['/productos']);

      });

    }

  }

}


