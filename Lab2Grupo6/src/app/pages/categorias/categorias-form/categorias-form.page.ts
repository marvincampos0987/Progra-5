import { Component, OnInit, inject } from '@angular/core';

import { FormBuilder, FormGroup, Validators } from '@angular/forms';

import { ActivatedRoute, Router } from '@angular/router';

import { CategoriaService } from '../../../services/categoria.service';

import { Categoria } from '../../../models';



@Component({

  selector: 'app-categorias-form',

  templateUrl: './categorias-form.page.html',

  styleUrls: ['./categorias-form.page.scss'],

  standalone: false,

})

export class CategoriasFormPage implements OnInit {

  form!: FormGroup;

  esEdicion = false;

  id?: number;



  private fb = inject(FormBuilder);

  private categoriaService = inject(CategoriaService);

  private route = inject(ActivatedRoute);

  private router = inject(Router);



  ngOnInit() {

    this.form = this.fb.group({

      nombreCategoria: ['', Validators.required],

      activo: [true],

    });



    const idParam = this.route.snapshot.paramMap.get('id');

    if (idParam) {

      this.esEdicion = true;

      this.id = +idParam;

      this.categoriaService.obtenerPorId(this.id).subscribe((data) => {

        if (data) {

          this.form.patchValue(data);

        }

      });

    }

  }



  guardar() {

    if (this.form.invalid) return;

    const categoria = this.form.value as Categoria;



    if (this.esEdicion && this.id) {

      categoria.categoriaId = this.id;

      this.categoriaService.actualizar(this.id, categoria).subscribe(() => {

        this.router.navigate(['/categorias']);

      });

    } else {

      this.categoriaService.crear(categoria).subscribe(() => {

        this.router.navigate(['/categorias']);

      });

    }

  }

}


