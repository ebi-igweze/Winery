import { Component, OnInit } from '@angular/core';
import { Popup } from '../../../../shared/classes/popup';
import { PopupService } from '../../../../shared/services/popup.service';
import { Category } from '../../../../app.models';
import { copy } from '../../../../app.config';
import { CategoryService } from '../../../../shared/services/category.service';
import { ProcessorService } from '../../../../shared/services/processor.service';

@Component({
    selector: '.app-category',
    templateUrl: './category.component.html',
    styles: []
})
export class CategoryComponent extends Popup implements OnInit {
    private type: 'add' | 'edit' = 'add';
    private category: Category;
    private categoryForm = { 
        name: null, 
        description: null, 
        isValid: function() { return this.name && this.description && this.name !== this.description; }
    };

    constructor(private cs: CategoryService, private ps: PopupService, private processor: ProcessorService) { super('category-item'); }  

    public ngOnInit(): void {
        let params = <{ type: 'add' | 'edit', category: Category}> this.ps.getParams();
        this.type = params.type;
        if (params.type === 'edit') {
            this.category = params.category; 
            this.categoryForm.name = this.category.name;
            this.categoryForm.description = this.category.description;
        }
    }

    public saveChanges(): void {
        this.hidePopup();
        if (this.type === 'add') this.addInfo();
        else this.editInfo();
    }

    private addInfo(): void {
        // let promise = this.cs.addCategory(this.categoryForm);
        // promise.then(console.log);
        this.processor.start('Adding new wine category');
        setTimeout(() => this.processor.stop('Category added sucessfully'), 5000)
    }

    private editInfo(): void {
        // only update the changes that have been made
        let name = this.category.name === this.categoryForm.name ? null : this.categoryForm.name;
        let description = this.category.description === this.categoryForm.description ? null : this.categoryForm.description;
        let info = { name: name, description: description };
        let promise = this.cs.editCategory(this.category.id, info);
        promise.then(console.log);
    }
}
