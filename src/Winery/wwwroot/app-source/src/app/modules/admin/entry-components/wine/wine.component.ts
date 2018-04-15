import { Component, OnInit } from '@angular/core';
import { Popup } from '../../../../shared/classes/popup';
import { CategoryService } from '../../../../shared/services/category.service';
import { WineService } from '../../../../shared/services/wine.service';
import { Category, Wine } from '../../../../app.models';
import { PopupService } from '../../../../shared/services/popup.service';

@Component({
    selector: '.app-wine',
    templateUrl: './wine.component.html',
    styles: []
})
export class WineComponent extends Popup implements OnInit {
    public categories: Category[];
    public category: string;
    private wine: Wine;
    private wineForm = { 
        name: null, 
        categoryId: null,
        description: null, 
        price: null,
        quantity: null, 
        year: null, 
        imagePath: null,
        isValid: function() { this.name && this.description && this.name !== this.description }
    }
    private type: 'add' | 'edit';
    constructor(private ps: PopupService, private cs: CategoryService, private ws: WineService) { super('wine-item');}

    public ngOnInit(): void {
        this.cs.getCategories().then(categories => this.categories = categories);
        let params = <{type: 'add' | 'edit', category: string, wine: Wine}> this.ps.getParams();
        this.type = params.type;
        this.category = params.category;
        if (this.type === 'edit') {
            this.wine = params.wine;
            this.wineForm.name = this.wine.name
            this.wineForm.year = this.wine.year
            this.wineForm.price = this.wine.price
            // this.wineForm.quantity = this.wine.quantity
            this.wineForm.imagePath = this.wine.imagePath
            this.wineForm.description = this.wine.description
        }
        
    }

    public addWine(): void {
        this.ws.addWine(this.category, this.wineForm)
               .then(() => this.hidePopup())
    }

    public editWine(): void {
        this.ws.editWine(this.category, this.wine.id, this.wineForm)
               .then(() => this.hidePopup());
    }

    public saveChanges(): void {
        if (this.wineForm.isValid())
            if (this.type === 'add') this.addWine();
            else this.editWine();
    }

}
