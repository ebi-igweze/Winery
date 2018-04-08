import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { WineComponent } from './components/wine/wine.component';
import { WineDetailsComponent } from './components/wine-details/wine-details.component';
import { CartService } from './services/cart.service';
import { Routes, RouterModule } from '@angular/router';
import { HomeComponent } from './home.component';

const childRoutes: Routes = [
    { path: 'wines/:id/details', component: WineDetailsComponent },
    { path: 'wines', component: WineComponent },
    { path: '', redirectTo: 'wines', pathMatch: 'full' },
]

const routes: Routes = [
    { path: '', component: HomeComponent, children: childRoutes }
]

@NgModule({
    imports: [
        CommonModule,
        RouterModule.forChild(routes)
    ],
    declarations: [
        WineComponent, WineDetailsComponent, HomeComponent 
    ],
    providers: [CartService],
    exports: [RouterModule]
})
export class HomeModule { }