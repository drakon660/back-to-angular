import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { BehaviorSubject, catchError, map, Observable, of, tap } from 'rxjs';
import { LoginResponse, User, UserCredentials } from './user';
import { environment } from '../environments/environment';
import { TokenService } from './token-service';

@Injectable({
  providedIn: 'root',
})
export class UserService {
  private user: BehaviorSubject<User | null>;

  constructor(private http: HttpClient, private tokenService: TokenService) {
    this.user = new BehaviorSubject<User | null>(null);
  }

  signIn(credentials: UserCredentials): Observable<User> {
    return this.http.post<User>('/api/sign-in', credentials).pipe(
      map((user: User) => {
        console.log('User-2:', user);
        this.user.next(user);
        return user;
      })
    );
  }

  signOut() {
    return this.http.post('/api/sign-out', null).subscribe(() => {
      this.user.next(null);
    });
  }

  isAuthenticated(): Observable<User | null> {
    return this.http.get<User>('/api/me', { withCredentials: true }).pipe(
      tap((result) => {
        console.log('Authenticated user:', result);
        this.user.next(result);
        return result;
      }),
      map((result) => result),
      catchError((error: HttpErrorResponse) => {
        if (error.status === 401) {
          console.warn('Unauthorized - redirecting to login');
        } else {
          console.error('Unexpected error in AuthGuard:', error);
        }
        return of(null);
      })
    );
  }

  getCurrentUser(): Observable<User | null> {
    return this.user.asObservable();
  }
}
