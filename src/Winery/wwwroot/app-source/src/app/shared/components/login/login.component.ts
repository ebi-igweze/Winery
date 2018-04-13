import { Component, OnInit } from '@angular/core';
import { AuthResponse } from '../../../app.models';
import { AuthService } from '../../services/auth.service';
import { HttpErrorResponse } from '@angular/common/http';
import { Router } from '@angular/router';

@Component({
    selector: '.app-login',
    templateUrl: './login.component.html',
    styles: []
})
export class LoginComponent {

    public userName: string;
    public password: string;
    public processing = false;
    public errorMsg = "";
    public hideMsg = (evt: MouseEvent) => { this.errorMsg = ""; evt.stopPropagation(); }

    constructor(private auth: AuthService) {}

    public login(): void {
        this.processing = true;
        let model = {userName: this.userName, password: this.password};
        this.auth.login(model).then(this.handleSuccess, this.handleError);
    }

    public handleSuccess = (res) => {
        console.log(res)
        this.processing = false;
    }

    public handleError = (error: HttpErrorResponse) => {
        this.processing = false;
        this.errorMsg = error.error;
    }

}

@Component({
    selector: ".logout",
    template: ""
})
export class LogoutComponent implements OnInit {
    constructor(private auth: AuthService, private router: Router) {}

    public ngOnInit(): void {
        this.auth.logOut().then(() => {
            this.router.navigate(["/"]);
        })
    }
}