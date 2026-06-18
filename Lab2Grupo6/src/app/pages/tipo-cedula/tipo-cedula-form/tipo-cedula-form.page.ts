import { Component, OnInit, inject } from '@angular/core';

import { FormBuilder, FormGroup, Validators } from '@angular/forms';

import { ActivatedRoute, Router } from '@angular/router';

import { TipoCedulaService } from '../../../services/tipo-cedula.service';

import { TipoCedula } from '../../../models';



@Component({

  selector: 'app-tipo-cedula-form',

  templateUrl: './tipo-cedula-form.page.html',

  styleUrls: ['./tipo-cedula-form.page.scss'],

  standalone: false,

})

export class TipoCedulaFormPage implements OnInit {

  form!: FormGroup;

  esEdicion = false;

  id?: number;



  private fb = inject(FormBuilder);

  private tipoCedulaService = inject(TipoCedulaService);

  private route = inject(ActivatedRoute);

  private router = inject(Router);



  ngOnInit() {

    this.form = this.fb.group({

      descripcion: ['', Validators.required],

    });



    const idParam = this.route.snapshot.paramMap.get('id');

    if (idParam) {

      this.esEdicion = true;

      this.id = +idParam;

      this.tipoCedulaService.obtenerPorId(this.id).subscribe((data) => {

        if (data) {

          this.form.patchValue(data);

        }

      });

    }

  }



  guardar() {

    if (this.form.invalid) return;

    const tipo = this.form.value as TipoCedula;



    if (this.esEdicion && this.id) {

      tipo.tipoCedula1 = this.id;

      this.tipoCedulaService.actualizar(this.id, tipo).subscribe(() => {

        this.router.navigate(['/tipo-cedula']);

      });

    } else {

      this.tipoCedulaService.crear(tipo).subscribe(() => {

        this.router.navigate(['/tipo-cedula']);

      });

    }

  }

}


