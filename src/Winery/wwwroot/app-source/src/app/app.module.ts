import { BrowserModule } from '@angular/platform-browser';
import { NgModule } from '@angular/core';
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
import { PsLinkDirective } from './shared/directives/ps-link.directive';
import { PopupService } from './shared/services/popup.service';
import { WineService } from './shared/services/wine.service';
import { ProcessorService } from './shared/services/processor.service';
import { RouteparamsService } from './shared/services/routeparams.service';

const $TokenInterceptor = { provide: HTTP_INTERCEPTORS, useClass: TokenInterceptor, multi: true };

@NgModule({
    declarations: [
        AppComponent, CartComponent, LoginComponent, LogoutComponent,
        SignupComponent, HeaderComponent, NotFoundComponent,
    ],
    imports: [
        BrowserModule, FormsModule, HttpClientModule, AppRoutesModule,
        JwtModule.forRoot({
            config: {
                tokenGetter: () => localStorage.getItem(keys.token)
            }
        }),
    ],
    providers: [ AuthService, UserService, $TokenInterceptor, WineService, CategoryService, PopupService, ProcessorService, RouteparamsService ],
    bootstrap: [ AppComponent ]
})
export class AppModule { }
