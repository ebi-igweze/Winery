import { Component, OnInit } from '@angular/core';
import { CategoryService } from '../../../../shared/services/category.service';
import { Category } from '../../../../app.models';

@Component({
    selector: 'app-categories',
    templateUrl: './categories.component.html',
    styles: []
})
export class CategoriesComponent implements OnInit {
    private categories: Category[];
    
    constructor(private cs: CategoryService) { }

    public ngOnInit(): void {
        this.cs.getCategories().then(c => this.categories = c);
    }
}
