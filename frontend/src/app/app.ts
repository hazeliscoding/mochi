import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

/** Root host. The dashboard shell and the auth pages are routed below this. */
@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  template: '<router-outlet />',
})
export class App {}
