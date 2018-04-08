import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CategoriesComponent } from './components/categories/categories.component';
import { WinesComponent } from './components/wines/wines.component';
import { Routes, RouterModule } from '@angular/router';
import { AdminComponent } from './admin.component';

const routes: Routes = [
    { path: 'categories', component: CategoriesComponent },
    { path: 'wines', component: WinesComponent },
    { path: '', redirectTo: 'categories', pathMatch: 'full' },
]

@NgModule({
  imports: [
    CommonModule,
    RouterModule.forChild(routes)
  ],
  declarations: [CategoriesComponent, WinesComponent, AdminComponent],
  exports: [RouterModule]
})
export class AdminModule { }
