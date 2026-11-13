import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

/** Componente raiz: só a página atual. */
@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet],
  template: '<router-outlet />',
})
export class AppComponent {}
