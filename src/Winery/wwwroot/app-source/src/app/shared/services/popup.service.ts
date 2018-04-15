import { Injectable, Type, ViewContainerRef, ComponentFactoryResolver } from '@angular/core';
import { Subject } from 'rxjs/Subject';

export type PopupStates = PopupState[];

export type PopupState = {
    name: string,
    params?: any,
    selectors: string[],
    component: Type<any>,
}

export type Popup = { 
    name: string, 
    vc: ViewContainerRef, 
    state: PopupState
}

@Injectable()
export class PopupService {
    private popups: Popup[] = [];
    private currentState: PopupState;
    public oncomponentloaded: Subject<{state: PopupState, vc: ViewContainerRef}> = new Subject();

    constructor(private componentFactoryResolver: ComponentFactoryResolver) { }

    public setupViewContainer(vc: ViewContainerRef, states: PopupStates): void {
        let newpopups = states.map(s =>{ return { name: s.name, vc: vc, state: s }; });
        this.popups = this.popups.concat(newpopups);
    }

    public removeViewContainer(vc: ViewContainerRef): void {
        // remove any popup using given view container ref
        this.popups = this.popups.filter(p => p.vc !== vc);
    }

    public getParams(): { [key: string]: any } {
        return this.currentState.params || {};
    }

    private loadComponent(state: PopupState, params: any, vc: ViewContainerRef, onsuccess: Function): void {
        // clear content of view container
        vc.clear();

        // get component's factory
        let componentFactory = this.componentFactoryResolver.resolveComponentFactory(state.component);

        // set current state
        this.currentState = state;

        // set current params
        this.currentState.params = params || state.params;

        // create and load component
        vc.createComponent(componentFactory);
        
        // call on success
        onsuccess();

        // notify component loaded subscribers
        this.oncomponentloaded.next({state: state, vc: vc});
    }

    public gotoState(name: string, params?: any): Promise<void> {
        let promiseHandler = (resolve, reject) => {
            let popup = this.popups.filter(ps => ps.name === name)[0];
            if (!popup) reject(`Popup State with name ${name} does not exist.`);
            else this.loadComponent(popup.state, params, popup.vc, resolve);
        }
        
        return new Promise<void>(promiseHandler);
    }


}
