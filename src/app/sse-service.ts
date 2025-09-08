// sse.service.ts
import { Injectable, NgZone } from '@angular/core';
import { Observable, Subject } from 'rxjs';
import { environment } from './environments/environment';
import { UserService } from './user/user-service';

interface Expired {
  cookieExpired: boolean;
}

@Injectable({
  providedIn: 'root',
})
export class SseService {
  private eventSource?: EventSource;
  private subject?: Subject<Expired>;

  constructor(private zone: NgZone, private userService: UserService) {}

  listenToCookieExpiry(): Observable<Expired> {
    if (this.subject) {
      return this.subject.asObservable(); // Already connected
    }

    this.subject = new Subject<Expired>();

    this.eventSource = new EventSource(`${environment.apiUrl}/api/notifications/sse`, {
      withCredentials: true
    });

    this.eventSource.addEventListener('cookieExpired', (event: MessageEvent) => {
      this.zone.run(() => {
        console.log('Received cookieExpired event:', event.data);
        const data: Expired = JSON.parse(event.data);
        this.subject!.next(data);
      });
    });

    this.eventSource.onerror = error => {
      this.zone.run(() => {
        this.subject?.error(error);
        this.close(); // Optionally close on error
      });
    };

    return this.subject.asObservable();
  }

  close(): void {
    this.eventSource?.close();
    this.eventSource = undefined;
    this.subject?.complete();
    this.subject = undefined;
    this.userService.signOut();
  }

  reconnect(): Observable<Expired> {
    this.close(); // make sure old connection is gone
    return this.listenToCookieExpiry();
  }
}