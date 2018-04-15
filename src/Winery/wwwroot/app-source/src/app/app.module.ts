import { BrowserModule } from '@angular/platform-browser';
import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms'
import { HttpClientModule, HTTP_INTERCEPTORS} from '@angular/common/http'

import { JwtModule } from '@auth0/angular-jwt';
import { AppComponent } from './app.component';
import { LoginComponent, LogoutComponent } from './shared/components/login/login.component';
import { SignupComponent } from './shared/components/signup/signup.component';
import { AuthService, TokenInterceptor } from './shared/services/auth.service';
import { UserService } from './shared/services/user.service';
import { HeaderComponent } from './shared/components/header/header.component';
import { NotFoundComponent } from './shared/components/not-found/not-found.component';
import { AppRoutesModule } from './app.routes';
import { CartComponent } from './shared/components/cart/cart.component';
import { keys } from './app.config';
import { CategoryService } from './shared/services/category.service';
import { PopupDirective } from './shared/directives/popup.directive';
import { PopupComponent } from './shared/components/popup/popup.component';

const $TokenInterceptor = { provide: HTTP_INTERCEPTORS, useClass: TokenInterceptor, multi: true };

@NgModule({
    declarations: [
        AppComponent, CartComponent, LoginComponent, LogoutComponent,
        SignupComponent, HeaderComponent, NotFoundComponent, PopupDirective, PopupComponent
    ],
    imports: [
        BrowserModule,
        FormsModule,
        CommonModule,
        HttpClientModule,
        AppRoutesModule,
        JwtModule.forRoot({
            config: {
                tokenGetter: () => localStorage.getItem(keys.token)
            }
        })
    ],
    providers: [ AuthService, UserService, $TokenInterceptor, CategoryService ],
    bootstrap: [ AppComponent ]
})
export class AppModule { }
