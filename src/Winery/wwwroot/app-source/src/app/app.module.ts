import { BrowserModule } from '@angular/platform-browser';
import { NgModule } from '@angular/core';

import { FormsModule } from '@angular/forms'
import { AppComponent } from './app.component';
import { LoginComponent } from './shared/components/login/login.component';
import { SignupComponent } from './shared/components/signup/signup.component';
import { AuthService } from './shared/services/auth.service';
import { UserService } from './shared/services/user.service';
import { HeaderComponent } from './shared/components/header/header.component';
import { NotFoundComponent } from './shared/components/not-found/not-found.component';
import { AppRoutesModule } from './app.routes';
import { CartComponent } from './shared/components/cart/cart.component';

@NgModule({
    declarations: [
        AppComponent, CartComponent, LoginComponent,
        SignupComponent, HeaderComponent, NotFoundComponent
    ],
    imports: [
        BrowserModule,
        FormsModule,
        AppRoutesModule
    ],
    providers: [ AuthService, UserService ],
    bootstrap: [ AppComponent ]
})
export class AppModule { }
