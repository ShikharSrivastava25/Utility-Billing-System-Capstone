import { Component, signal } from '@angular/core';
import { Login } from '../login/login';
import { Register } from '../register/register';

@Component({
  selector: 'app-auth',
  imports: [Login, Register],
  templateUrl: './auth.html',
  styleUrl: './auth.css',
})
export class Auth {
  showLogin = signal(true);

  toggleView(): void {
    this.showLogin.update(value => !value);
  }
}
