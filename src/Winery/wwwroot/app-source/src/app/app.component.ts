import { Component, AfterViewInit } from '@angular/core';
import { WinehubService } from './shared/services/winehub.service';

@Component({
    selector: '.app-root',
    templateUrl: './app.component.html',
    styles: []
})
export class AppComponent implements AfterViewInit {
    
    constructor(private wineHub: WinehubService) {}

    public ngAfterViewInit(): void {
        this.wineHub.severSend("Message from browser")
    }
}
