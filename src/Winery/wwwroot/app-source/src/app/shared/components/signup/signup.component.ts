import { Component, OnInit } from '@angular/core';
import { AuthService } from '../../services/auth.service';
import { UserModel } from '../../../app.models';

@Component({
    selector: '.app-signup',
    templateUrl: './signup.component.html',
    styles: []
})
export class SignupComponent {
    public email: string;
    public lastName: string;
    public firstName: string;
    public password: string;

    constructor(private auth: AuthService) { }

    public signUp(): void {
        let userInfo: UserModel = {
            email: this.email,
            lastName: this.lastName,
            password: this.password,
            firstName: this.firstName
        }
        this.auth.signUp(userInfo);
    }
}
