import { Component, OnInit } from '@angular/core';
import { Popup } from '../../../../shared/classes/popup';
import { PopupService } from '../../../../shared/services/popup.service';
import { Category } from '../../../../app.models';
import { copy } from '../../../../app.config';
import { CategoryService } from '../../../../shared/services/category.service';
import { ProcessorService } from '../../../../shared/services/processor.service';
import { HttpErrorResponse } from '@angular/common/http';

@Component({
    selector: '.app-category',
    templateUrl: './category.component.html',
    styles: []
})
export class CategoryComponent extends Popup implements OnInit {
    private type: 'add' | 'edit' = 'add';
    private category: Category;
    public errorMsg:string;
    public processing = false;
    public categoryForm = { 
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
        if (this.type === 'add') this.addInfo();
        else this.editInfo();
    }

    private addInfo(): void {
        this.processing = true;
        this.cs.addCategory(this.categoryForm)
        .then(this.handleSuccess)
        .catch(this.handleError);
    }

    private editInfo(): void {
        this.processing = true;
        // only update the changes that have been made
        let name = this.category.name === this.categoryForm.name ? null : this.categoryForm.name;
        let description = this.category.description === this.categoryForm.description ? null : this.categoryForm.description;
        let info = { name: name, description: description };
        this.cs.editCategory(this.category.id, info)
        .then(this.handleSuccess)
        .catch(this.handleError);
    }
    
    public handleSuccess = () => {
        this.processing = false;
        this.hidePopup();
    }

    public handleError = (err: HttpErrorResponse) => {
        this.processing = false;
        this.errorMsg = err.error
    }
}
