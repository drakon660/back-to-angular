import { Component, signal, Signal, WritableSignal } from '@angular/core';
import { ProductDetails } from '../../product-details/product-details';
import { CartService } from '../../cart/cart-service';
import { Router } from '@angular/router';
import { ProductService } from '../../catalog/product-service';
import { Observable, of } from 'rxjs';
import { AsyncPipe } from '@angular/common';

@Component({
  selector: 'app-catalog',
  templateUrl: './squad-catalog.component.html',
  styleUrls: ['./squad-catalog.component.css'],
  providers: [],
  imports: [ProductDetails, AsyncPipe],
})
export class SquadCatalogComponent {
  //squad: WritableSignal<Product[]> = signal([]);
  squad: Observable<Product[]> = of([]);

  ngOnInit() {
    this.squad = this.productService.getProfiles();
  }

  constructor(private productService:ProductService, private cartService: CartService, private router: Router) {}

  addToCart(engineer: Product) {
    this.cartService.add(engineer);
    this.router.navigate(['/squad-cart']);
  }
}
