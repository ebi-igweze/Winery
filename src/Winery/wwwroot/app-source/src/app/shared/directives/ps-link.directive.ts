import { Directive, OnInit, ElementRef, Input, OnDestroy } from '@angular/core';
import { PopupService } from '../services/popup.service';

@Directive({
    selector: '[psLink]'
})
export class PsLinkDirective implements OnInit, OnDestroy {
    @Input() psLink: string;
    @Input() params: any;

    constructor(private el: ElementRef, private ps: PopupService) { }
    
    private click = (evt: MouseEvent) => {
        evt.preventDefault();
        this.ps.gotoState(this.psLink, this.params);
    }
    public ngOnInit(): void {
        this.el.nativeElement.addEventListener('click', this.click);
    }
    public ngOnDestroy(): void {
        this.el.nativeElement.removeEventListener('click', this.click);
    }

}
