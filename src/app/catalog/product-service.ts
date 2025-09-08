import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../environments/environment';

@Injectable({
  providedIn: 'root',
})
export class ProductService {
  constructor(private http: HttpClient) {}

  getProducts(): Observable<Product[]> {
    return this.http.get<Product[]>(`${environment.apiUrl}/api/products`);
  }

  getProfiles(): Observable<Product[]> {
    return this.http.get<Product[]>(`${environment.apiUrl}/api/profiles`);
  }
}
