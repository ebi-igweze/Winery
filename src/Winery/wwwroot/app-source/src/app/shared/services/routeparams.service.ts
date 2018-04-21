import { Injectable } from '@angular/core';
import { Subject } from 'rxjs/Subject';

@Injectable()
export class RouteparamsService {
    
    public onroutechange = new Subject<{[key: string]: string}>();

    private route: {[key: string]: string} = {};

    public setRouteParam(key: string, value: string) {
        this.route[key] = value;
        this.onroutechange.next(this.route);    
    }

    public get(key:  string): string {
        return this.route[key];
    }

}
