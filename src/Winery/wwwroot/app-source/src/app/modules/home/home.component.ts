import { Component } from '@angular/core';
import { CategoryService } from '../../shared/services/category.service';
import { Category } from '../../app.models';
import { RouteparamsService } from '../../shared/services/routeparams.service';
import { Router } from '@angular/router';

@Component({
    selector: '.app-home',
    templateUrl: './home.component.html',
    styles: []
})
export class HomeComponent {
    public current = <Category> {};

    constructor(public cs: CategoryService, private router: Router, private route: RouteparamsService) { 
        cs.getCategories();
        this.route.onroutechange.subscribe(() => this.setCurrent())
    }

    public setCurrent() {
        let categoryId = this.route.get('categoryId');
        if (categoryId !== 'all') this.cs.getCategory(categoryId).then(c => this.current = c);
        else this.current = <Category> {}
    }

    public goto(evt: MouseEvent, id: string) {
        evt.preventDefault();
        this.router.navigate(['/categories', id, 'wines']);
    }
    
}
