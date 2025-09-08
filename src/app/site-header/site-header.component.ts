import { Component, OnInit } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { UserService } from '../user/user-service';
import { User } from '../user/user';
import {
  HttpClient,
  HttpEvent,
  HttpEventType,
  HttpResponse,
} from '@angular/common/http';
import { filter, map, mapTo, Observable, of, Subscription, tap } from 'rxjs';
import { AsyncPipe, CommonModule } from '@angular/common';

@Component({
  selector: 'app-site-header',
  imports: [RouterLink, AsyncPipe, CommonModule],
  templateUrl: './site-header.component.html',
  styleUrls: ['./site-header.component.css'],
})
export class SiteHeaderComponent implements OnInit {
  user: Observable<User | null> = of(null);
  showSignOutMenu: boolean = false;
  dowloadSub?: Subscription;

  constructor(
    private userService: UserService,
    private httpclient: HttpClient,
    private router: Router
  ) {}

  ngOnInit() {
    this.user = this.userService.getCurrentUser();
     this.user.subscribe((user) => {
    console.log('User changed:', user);
  });
  }

  toggleSignOutMenu() {
    this.showSignOutMenu = !this.showSignOutMenu;
  }

  signOut() {
    this.userService.signOut();
    this.router.navigate(['/sign-in']);
  }

  abortDownload() {
    this.dowloadSub?.unsubscribe();
  }

  saveAsLink2(): (
    source: Observable<HttpEvent<Blob>>
  ) => Observable<HttpResponse<Blob>> {
    return (source: Observable<HttpEvent<Blob>>) =>
      source.pipe(
        tap((event) => {
          if (event.type === HttpEventType.DownloadProgress && event.total) {
            const percent = Math.round((100 * event.loaded) / event.total);
            console.log(`Download progress: ${percent}%`);
          }
        }),
        filter((event) => event instanceof HttpResponse),
        tap((event) => {
          const response = event as HttpResponse<Blob>;
          const blob = response.body;
          if (!blob) return;

          const url = window.URL.createObjectURL(blob);
          const a = document.createElement('a');
          a.href = url;
          a.download = 'downloadedFile.zip';
          a.click();
          window.URL.revokeObjectURL(url);
        })
      );
  }

  saveAsLink3(): void {
    const downloadUrl = '/api/download'; // your backend endpoint
    const a = document.createElement('a');
    a.href = downloadUrl;
    a.download = ''; // optional, backend should set Content-Disposition
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
  }

  saveAsLink(
    source: Observable<HttpEvent<Blob>>
  ): Observable<HttpResponse<Blob>> {
    return source.pipe(
      tap((event) => {
        if (event.type === HttpEventType.DownloadProgress && event.total) {
          const percent = Math.round((100 * event.loaded) / event.total);
          console.log(`Download progress: ${percent}%`);
        }
      }),
      filter((event) => event instanceof HttpResponse),
      tap((event) => {
        const response = event as HttpResponse<Blob>;
        const blob = response.body;
        if (!blob) return;

        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = 'downloadedFile.zip';
        a.click();
        window.URL.revokeObjectURL(url);
      })
    );
  }

  download() {
    this.dowloadSub = this.httpclient
      .request('GET', 'http://localhost:5042/api/download/download-file', {
        responseType: 'blob',
        reportProgress: true,
        observe: 'events',
        keepalive: true,
      })
      .pipe(this.saveAsLink)
      .subscribe({
        complete: () => console.log('Download complete'),
        error: (err) => console.error('Download error', err),
      });
  }
}
