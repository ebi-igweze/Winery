import { Injectable } from '@angular/core';
import { AuthService } from './auth.service';
import { Userstatus } from '../../app.models';
import { keys } from '../../app.config';

@Injectable()
export class UserService {
    public status: Userstatus;
    public isAdmin: boolean;
    
    constructor(private auth: AuthService) { 
        let checkAdmin = () => {
            let role = auth.isAuthenticated() ? localStorage.getItem(keys.role) : null;
            return role && role === 'admin';
        }
        this.isAdmin = checkAdmin();
        this.status = auth.isAuthenticated() ? Userstatus.loggedIn : Userstatus.loggedOut;
        auth.onlogin.subscribe(status => { this.status = status; this.isAdmin = checkAdmin(); }); 
        auth.onlogout.subscribe(status => { this.status = status; this.isAdmin = false; });
    }    

    public logout = () => this.auth.logOut();
}