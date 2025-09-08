import { CanActivate, Router, Routes } from '@angular/router';
import { Home } from './home/home';
import { Cart } from './cart/cart';
import { Catalog } from './catalog/catalog';
import { SignIn } from './user/sign-in/sign-in';
import { SquadCatalogComponent } from './squad/squad-catalog/squad-catalog.component';
import { SearchComponent } from './catalog/search/search.component';
import { UserComponent } from './user/user-add';
import { Injectable } from '@angular/core';
import { catchError, map, Observable, of, tap } from 'rxjs';
import { UserService } from './user/user-service';
import { HttpErrorResponse } from '@angular/common/http';


@Injectable({ providedIn: 'root' })
export class AuthGuard implements CanActivate {
  constructor(private authService: UserService, private router: Router) {}

 canActivate(): Observable<boolean> {
  return this.authService.isAuthenticated().pipe(
    tap((user) => {
      if (!user) {
        this.router.navigate(['/sign-in']);
      }
    }),
    map((user) => !!user)
  );
}
}

export const routes: Routes = [
  {
    path: '',
    redirectTo: '/home',
    pathMatch: 'full',
  },
  {
    path: 'home',
    component: Home
  },
   {
    path: 'user-add',
    component: UserComponent,
    canActivate: [AuthGuard],
  },
  {
    path: 'cart',
    component: Cart,
    canActivate: [AuthGuard],
  },
  {
    path: 'catalog/:filter',
    component: Catalog,
    canActivate: [AuthGuard],
  },
  {
    path: 'catalog',
    component: Catalog,
    canActivate: [AuthGuard],
  },
  {
    path: 'sign-in',
    component: SignIn,
  },
  { path: 'squad', component: SquadCatalogComponent },
  {
    path: 'squad-cart',
    component: Cart,
    title: "Squad Cart - Joe's Robot Shop",
  },
  { path: 'search', component: SearchComponent, canActivate: [AuthGuard], },
  
];

