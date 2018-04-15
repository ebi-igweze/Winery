import { Component, OnInit, Input, ViewChild, ElementRef, ViewContainerRef } from '@angular/core';
import { PopupStates, PopupService, PopupState } from '../../services/popup.service';
import { PopupDirective } from '../../directives/popup.directive';
import { Subscription } from 'rxjs/Subscription';

@Component({
    selector: '.app-popup',
    template: `<ng-template app-popup></ng-template>`, // app-popup is for directive
    styles: []
})
export class PopupComponent implements OnInit {

    @Input() states: PopupStates;
    @ViewChild(PopupDirective) childContainer: PopupDirective;
    private currentSelectors: string[] = [];
    private componentloadedSubscription: Subscription;

    constructor(private ps: PopupService, private el: ElementRef) { }

    public ngOnInit(): void {
        this.ps.setupViewContainer(this.childContainer.viewContainerRef, this.states)
        this.componentloadedSubscription = this.ps.oncomponentloaded.subscribe(stateAndView => {
            // current popup state
            let state = stateAndView.state;
            // current popup container
            let vc = stateAndView.vc;
            
            // return if it is not the view child of this component
            if (this.childContainer.viewContainerRef !== vc) return;
            
            let className = (<HTMLDivElement> this.el.nativeElement).className;

            // remove selectors for previous components
            className = className.split(' ').filter($class => this.currentSelectors.includes($class) === false).join(' ');
            
            // if new component selectors, add selectors for new component
            if (state.selectors.length) className = className.concat(' ', state.selectors.join(' '));

            // assign new component className
            this.el.nativeElement.className = className;

            // change current selector to new component selectors
            this.currentSelectors = state.selectors;
        });
    }

    public ngOnDestroy(): void {
        this.componentloadedSubscription.unsubscribe();
        this.ps.removeViewContainer(this.childContainer.viewContainerRef);
    }

}
