import { Component } from '@angular/core';
import { UserCredentials } from '../user';
import { UserService } from '../user-service';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { SseService } from '../../sse-service';
import { AuthGuardService } from '../../AuthGuardService';
import { environment } from '../../environments/environment';


@Component({
  selector: 'app-sign-in',
  imports: [FormsModule],
  templateUrl: './sign-in.html',
  styleUrl: './sign-in.css',
})
export class SignIn {
  credentials: UserCredentials = { userName: '', password: ''};
  signInError: boolean = false;

  constructor(
    private userService: UserService,
    private router: Router,
    private authGuardService: AuthGuardService,    
  ) {}

  signIn() {  
  
    this.userService.signIn(this.credentials).subscribe({
      error: () => {
        console.log('sign in error');
      },
      next: (user) => {
        console.log('sign in success');
        //window.location.reload();
      },
      complete: () => {
        this.signInError = false;
         this.authGuardService.startCookieExpiryListener();
        //  if(environment.useCookie)
        //  

        this.router.navigate(['/catalog']);
      },
    });  
  }
}
