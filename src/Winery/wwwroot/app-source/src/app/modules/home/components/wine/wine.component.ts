import { Component, OnInit, AfterViewInit, OnDestroy } from '@angular/core';
import { WineService } from '../../../../shared/services/wine.service';
import { Wine } from '../../../../app.models';
import { ActivatedRoute, Router, NavigationEnd } from '@angular/router';
import { RouteparamsService } from '../../../../shared/services/routeparams.service';
import { Subscription } from 'rxjs/Subscription';

@Component({
    selector: 'app-wine',
    templateUrl: './wine.component.html',
    styles: []
})
export class WineComponent implements AfterViewInit, OnDestroy {
    public wines: Wine[];
    private subscription: Subscription;

    constructor(private ws: WineService, private router: Router, private route: ActivatedRoute, private routeparam: RouteparamsService) { 
        this.getWines();
    }

    public ngAfterViewInit(): void {
       this.subscription = this.router.events.subscribe(evt => {
            if (evt instanceof NavigationEnd) {
                this.getWines();
            }
        })
    }

    public ngOnDestroy(): void {
        this.subscription.unsubscribe();
    }

    public getWines(): void {
        let categoryId = this.route.snapshot.paramMap.get('categoryId');
        this.routeparam.setRouteParam('categoryId', categoryId);
        if (categoryId)
            if (categoryId === 'all') this.ws.getAllWines().then(wines => this.wines = wines); 
            else this.ws.getWines(categoryId).then(wines => this.wines = wines);
    }
}
