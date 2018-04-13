import { Injectable } from '@angular/core';
import { AuthService } from './auth.service';
import { Userstatus } from '../../app.models';

@Injectable()
export class UserService {
    public status: Userstatus;
    constructor(private auth: AuthService) { 
        this.status = auth.isAuthenticated() ? Userstatus.loggedIn : Userstatus.loggedOut;
        auth.onlogin.subscribe(status => this.status = status); 
        auth.onlogout.subscribe(status => this.status = status);
    }    

    public logout = () => this.auth.logOut();
}
