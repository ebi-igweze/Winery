import { Injectable } from '@angular/core';
import { HttpClient, HttpInterceptor, HttpHandler, HttpRequest } from '@angular/common/http';
import { LoginModel, AuthResponse, UserModel, Userstatus } from '../../app.models';
import { api, keys } from '../../app.config';
import { Subject } from 'rxjs/Subject';
import { Observable } from 'rxjs/Observable';
import 'rxjs/add/operator/do';
import { JwtHelperService } from '@auth0/angular-jwt';
import { Router } from '@angular/router';

@Injectable()
export class AuthService {
    public onlogin = new Subject<Userstatus>();
    public onlogout = new Subject<Userstatus>();

    constructor(private http: HttpClient, private jwt: JwtHelperService, private router: Router) { }

    public isAuthenticated(): boolean {
        let token = localStorage.getItem(keys.token) || null;

        // check whether token exist and if expired
        return  token !== null  && !this.jwt.isTokenExpired(token);
    }

    public login(info: LoginModel) : Promise<AuthResponse> {
        let promise = this.http.post<AuthResponse>(api.login, info).toPromise();
        promise.then(this.storeToken, this.handleError);
        return promise;
    }

    public signUp(info: UserModel) : Promise<AuthResponse> {
        let promise = this.http.post<AuthResponse>(api.signUp, info).toPromise();
        promise.then(this.storeToken, this.handleError);
        return promise;
    }

    public logOut(): Promise<{}> {
        let promise = Promise.resolve({}); //this.http.post(api.logout, {}).toPromise();
        promise.then(this.removeToken)
               .catch(this.handleError);
        return promise;
    }

    private handleError(error) {
        console.log("Auth Error: ", error);
    }

    private storeToken = (res: AuthResponse) => {
        let role = this.jwt.decodeToken(res.token)[keys.role];
        localStorage.setItem(keys.token, res.token);
        localStorage.setItem(keys.role, role);
        this.onlogin.next(Userstatus.loggedIn);
    }

    private removeToken = () => {
        localStorage.removeItem(keys.token);
        localStorage.removeItem(keys.role);
        this.onlogout.next(Userstatus.loggedOut);
        return this.router.navigate(['/'])
    }
}


export class TokenInterceptor implements HttpInterceptor {

    public intercept(req: HttpRequest<any>, next: HttpHandler): Observable<any> {
        let header = { 'Authorization': `Bearer ${localStorage.getItem(keys.token)}`}
        let clone = req.clone({setHeaders: header});
        
        // get observable response
        let observable = next.handle(clone);
        
        // catch all exceptions
        observable.do(res => console.log("Success: ", res), err => console.log("Error: ", err));
        return observable; 
    }
}