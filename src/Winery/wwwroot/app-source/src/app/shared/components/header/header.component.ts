import { Component, OnInit } from '@angular/core';
import { UserService } from '../../services/user.service';

@Component({
    selector: 'app-header',
    templateUrl: './header.component.html',
    styles: []
})
export class HeaderComponent {

    constructor(private user: UserService) { }

    public logout (evt: MouseEvent) {
        this.user.logout();
        evt.preventDefault();
        evt.stopPropagation();
    }

}
