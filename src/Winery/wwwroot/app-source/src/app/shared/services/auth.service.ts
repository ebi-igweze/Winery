import { Injectable } from '@angular/core';
import { HttpClient, HttpInterceptor, HttpHandler, HttpRequest } from '@angular/common/http';
import { LoginModel, AuthResponse, UserModel, Userstatus } from '../../app.models';
import { api, keys } from '../../app.config';
import { Subject } from 'rxjs/Subject';
import { Observable } from 'rxjs/Observable';
import 'rxjs/add/operator/do';

@Injectable()
export class AuthService {
    public onlogin = new Subject<Userstatus>();
    public onlogout = new Subject<Userstatus>();

    constructor(private http: HttpClient) { }

    public isAuthenticated(): boolean {
        let token = localStorage.getItem(keys.token) || null;

        // check whether token exist and if expired
        return  token !== null // && !this.jwtHelper.isTokenExpired(token);
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
        let promise = this.http.post(api.logout, {}).toPromise();
        promise.then(this.removeToken)
               .catch(this.handleError);
        return promise;
    }

    private handleError(error) {
        console.log("Auth Error: ", error);
    }

    private storeToken = (res: AuthResponse) => {
        localStorage.setItem(keys.token, res.token);
        this.onlogin.next(Userstatus.loggedIn);
    }

    private removeToken = () => {
        localStorage.removeItem(keys.token);
        this.onlogout.next(Userstatus.loggedOut);
    }
}


export class TokenInterceptor implements HttpInterceptor {

    public intercept(req: HttpRequest<any>, next: HttpHandler): Observable<any> {
        let headers = {
            // 'Content-type': 'application/json',
            'Authorization': `Bearer ${localStorage.getItem(keys.token)}`
        }
        console.log(`Setting header for: ${req.url}`)
        
        let clone = req.clone({setHeaders: headers})
        let observable = next.handle(clone);

        // catch all exceptions
        observable.do(res => console.log("Success: ", res), err => console.log("Error: ", err));
        return observable; 
    }
}