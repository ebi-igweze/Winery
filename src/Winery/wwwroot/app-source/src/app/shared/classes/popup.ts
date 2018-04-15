import { AfterViewInit, OnDestroy } from "@angular/core";

export class Popup implements AfterViewInit, OnDestroy {
    private popup: HTMLDivElement;
    private modalBackdrop: HTMLDivElement;

    constructor(private popupClassName: string) {
        this.modalBackdrop = document.createElement('div');
        document.body.appendChild(this.modalBackdrop);
    }
    
    public ngAfterViewInit(): void {
        this.popup = <HTMLDivElement> document.getElementsByClassName(this.popupClassName)[0];
        this.showPopup();
    } 

    public ngOnDestroy(): void {
        this.hidePopup();
        document.body.removeChild(this.modalBackdrop);
    }

    public showPopup(): void {
        document.body.classList.add('modal-open');
        this.popup.style.display = 'block';
        this.popup.classList.add('open')
        this.modalBackdrop.className = "modal-backdrop show";
    }

    public hidePopup(): void {
        document.body.classList.remove('modal-open');
        this.modalBackdrop.className = '';
        this.popup.classList.remove('open');
        this.popup.style.display = 'none';
    }
}
