import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms'
import { PopupComponent } from './components/popup/popup.component';
import { PsLinkDirective } from './directives/ps-link.directive';
import { PopupDirective } from './directives/popup.directive';

@NgModule({
  imports: [ CommonModule, FormsModule ],
  declarations: [ PopupComponent, PsLinkDirective, PopupDirective ],
  exports: [ PopupComponent, PsLinkDirective, PopupDirective, CommonModule, FormsModule ],
})
export class SharedModule { }
