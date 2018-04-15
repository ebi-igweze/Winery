import { NgModule } from '@angular/core';
import { CategoriesComponent } from './components/categories/categories.component';
import { WinesComponent } from './components/wines/wines.component';
import { Routes, RouterModule } from '@angular/router';
import { AdminComponent } from './admin.component';
import { CategoryComponent } from './entry-components/category/category.component';
import { PopupComponent } from '../../shared/components/popup/popup.component';
import { PsLinkDirective } from '../../shared/directives/ps-link.directive';
import { PopupDirective } from '../../shared/directives/popup.directive';
import { SharedModule } from '../../shared/shared.module';
import { PopupService } from '../../shared/services/popup.service';
import { WineComponent } from './entry-components/wine/wine.component';

const routes: Routes = [
    { path: 'categories/:category/wines', component: WinesComponent },
    { path: 'categories', component: CategoriesComponent },
    { path: '', redirectTo: 'categories', pathMatch: 'full' },
]

@NgModule({
    imports: [
        RouterModule.forChild(routes),
        SharedModule
    ],
    declarations: [
        CategoriesComponent, WinesComponent, AdminComponent,
        CategoryComponent, WineComponent
    ],
    exports: [RouterModule],
    entryComponents: [ CategoryComponent, WineComponent ],
    providers: [ PopupService ]
})
export class AdminModule { }
