import { Component, OnInit } from '@angular/core';
import { WineService } from '../../../../shared/services/wine.service';
import { ActivatedRoute } from '@angular/router';
import { Wine, Category } from '../../../../app.models';
import { PopupStates } from '../../../../shared/services/popup.service';
import { WineComponent } from '../../entry-components/wine/wine.component';
import { CategoryService } from '../../../../shared/services/category.service';


@Component({
    selector: 'app-wines',
    templateUrl: './wines.component.html',
    styles: []
})
export class WinesComponent implements OnInit {
    public wines: Wine[];
    public category = "";
    public categories: Category[];
    public states: PopupStates = [{name: 'wine', component: WineComponent, selectors: ['wine-item']}];

    constructor(private cs: CategoryService, private ws: WineService, private activeRoute: ActivatedRoute) { }

    public ngOnInit(): void {
        this.category = this.activeRoute.snapshot.paramMap.get('category');
        this.ws.getWines(this.category).then(wines => this.wines = wines); 
        this.cs.getCategories().then(categories => this.categories = categories);       
    }

}
