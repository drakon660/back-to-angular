import { Router } from "@angular/router";
import { SseService } from "./sse-service";
import { Injectable } from "@angular/core";
import { debounce, debounceTime, filter, take } from "rxjs";

@Injectable({ providedIn: 'root' })
export class AuthGuardService {
    private isListening = false;

  constructor(
    private sseService: SseService,
    private router: Router
  ) {
    
  }

  public startCookieExpiryListener(): void {
     if (this.isListening) {
      return; // Prevent multiple listeners
    }

     this.isListening = true;

    this.sseService
      .listenToCookieExpiry()
      .pipe(
        debounceTime(1000), // Debounce to avoid rapid firing
        filter((msg) => msg.cookieExpired),
        take(1)
      )
      .subscribe(() => {
        this.handleCookieExpiry();
      });
  }

  private handleCookieExpiry(): void {
    this.isListening = false;
    this.sseService.close();
    this.router.navigate(['/sign-in']);
    // Optionally clear any stored auth tokens/data
  }
}