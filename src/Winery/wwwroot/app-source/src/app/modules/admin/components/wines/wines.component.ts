import { Component, OnInit } from '@angular/core';
import { WineService } from '../../../../shared/services/wine.service';
import { ActivatedRoute, Router } from '@angular/router';
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
    public states: PopupStates = [{name: 'wine', component: WineComponent, selectors: ['wine-item']}];

    constructor(private cs: CategoryService, private router: Router, private ws: WineService, private activeRoute: ActivatedRoute) { }

    public ngOnInit(): void {
        this.category = this.activeRoute.snapshot.paramMap.get('category');
        console.log(this.category);
        this.cs.getCategories()
            .then(categories => {
                let category = categories[0];
                this.category = this.category || category.id;
                return this.ws.getWines(this.category);
            })
            .then(wines => this.wines = wines); 
    }

    public deleteWine(wine: Wine, evt): void {
        let message = `Are you sure you want to delete this category: '${wine.name}'`;
        if (confirm(message)) this.ws.deleteWine(wine.id); 

        evt.preventDefault();
    }

    public getWines(): void {
        if (!this.category) return;
        else this.ws.getWines(this.category).then(w => this.wines = w);

        this.router.navigate(['/admin/categories/', this.category, 'wines']);
    }
}
