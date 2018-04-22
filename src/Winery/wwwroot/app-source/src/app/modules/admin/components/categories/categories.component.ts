import { Component, OnInit } from '@angular/core';
import { CategoryService } from '../../../../shared/services/category.service';
import { Category } from '../../../../app.models';
import { PopupStates } from '../../../../shared/services/popup.service';
import { CategoryComponent } from '../../entry-components/category/category.component';

@Component({
    selector: 'app-categories',
    templateUrl: './categories.component.html',
    styles: []
})
export class CategoriesComponent {
    public states: PopupStates = [{name: 'category', component: CategoryComponent, selectors: ['category-item']}]

    constructor(private cs: CategoryService) { }

    public deleteCategory(category: Category, evt): void {
        let message = `Are you sure you want to delete this category: '${category.name}'`;
        if (confirm(message)) this.cs.deleteCategory(category.id); 

        evt.preventDefault();
    }
}
