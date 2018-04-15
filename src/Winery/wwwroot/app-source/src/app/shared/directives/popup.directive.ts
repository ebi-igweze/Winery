import { Directive, ViewContainerRef } from '@angular/core';

@Directive({
  selector: '[app-popup]'
})
export class PopupDirective {

  constructor(public viewContainerRef: ViewContainerRef) { }

}
