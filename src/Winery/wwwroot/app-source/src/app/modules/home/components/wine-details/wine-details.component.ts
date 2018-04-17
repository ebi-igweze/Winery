import { Component, OnInit } from '@angular/core';
import { WineService } from '../../../../shared/services/wine.service';
import { Wine } from '../../../../app.models';
import { ActivatedRoute, Router } from '@angular/router';
import { RouteparamsService } from '../../../../shared/services/routeparams.service';

@Component({
    selector: 'app-wine-details',
    templateUrl: './wine-details.component.html',
    styles: []
})
export class WineDetailsComponent {
    public wine = <Wine> {}

    constructor(private ws: WineService, private router: Router, private route: ActivatedRoute, private routeparam: RouteparamsService) { 
        this.getWines();
    }

    public getWines(): void {
        let wineId = this.route.snapshot.paramMap.get('wineId');
        let categoryId = this.route.snapshot.paramMap.get('categoryId');
        this.routeparam.setRouteParam('categoryId', categoryId);
        this.ws.getWine(categoryId, wineId).then(wine => this.wine = wine);
    }
}
