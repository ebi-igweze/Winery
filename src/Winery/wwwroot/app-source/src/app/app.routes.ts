import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { NotFoundComponent } from './shared/components/not-found/not-found.component';

const routes: Routes = [
    {path: 'admin', loadChildren: 'app/modules/admin/admin.module#AdminModule' },
    {path: '', loadChildren: 'app/modules/home/home.module#HomeModule' },
    {path: '**', component: NotFoundComponent },
];

@NgModule({
  imports: [RouterModule.forRoot(routes, { useHash: true})],
  exports: [RouterModule]
})
export class AppRoutesModule { }
