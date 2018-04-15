import { Component, OnInit } from '@angular/core';
import { WineService } from '../../../../shared/services/wine.service';
import { ActivatedRoute } from '@angular/router';
import { Wine } from '../../../../app.models';
import { PopupStates } from '../../../../shared/services/popup.service';
import { WineComponent } from '../../entry-components/wine/wine.component';


@Component({
    selector: 'app-wines',
    templateUrl: './wines.component.html',
    styles: []
})
export class WinesComponent implements OnInit {
    public wines: Wine[];
    public states: PopupStates = [{name: 'wine', component: WineComponent, selectors: ['wine-item']}];

    constructor(private ws: WineService, private activeRoute: ActivatedRoute) { }

    public ngOnInit(): void {
        let category = this.activeRoute.snapshot.paramMap.get('category');
        this.ws.getWines(category).then(wines => this.wines = wines);        
    }

}
