import { Component } from '@angular/core';
import { SiteHeaderComponent } from './site-header/site-header.component';
import { Router, RouterOutlet } from '@angular/router';
import { SseService } from './sse-service';
import { debounceTime, filter, take, takeUntil } from 'rxjs';
import { AuthGuardService } from './AuthGuardService';
import { UserService } from './user/user-service';
import { environment } from './environments/environment';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, SiteHeaderComponent],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {
  protected title = 'back-to-angular';

  constructor(
    private authGuardService: AuthGuardService,
    private userService: UserService
  ) {}

  ngOnInit(): void {

    this.userService.isAuthenticated().subscribe((user) => {
      if (user) {
        console.log('User is authenticated');
        this.authGuardService.startCookieExpiryListener();
      }
    });
  }
}
