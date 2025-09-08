import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class CartService {
  private cart: BehaviorSubject<CartItem[]> = new BehaviorSubject<CartItem[]>([]);
  constructor(private http: HttpClient) { }

  getCart(): Observable<CartItem[]> {
    return this.cart;
  }

  add(product: Product) {
    console.log('Adding to cart: ' + product.name);

    let cartItems = this.cart.getValue();

    let cartItem = cartItems.find(i => i.product.id === product.id);
    if(cartItem){
      cartItem.quantity++;
      cartItem.total = cartItem.quantity * cartItem.product.price;
    }
    else{
      cartItems.push({ product, quantity: 1, total: product.price });
    }

    this.cart.next(cartItems);
    // this.http.post('/api/cart', cartItems).subscribe(() => {
    //   console.log('added ' + product.name + ' to cart!');
    // });
  }

  remove(product: Product) {
    console.log('Removing from cart: ' + product.name);
    let newCart = this.cart.getValue().filter((cartItem) => cartItem.product.id !== product.id);
    this.cart.next(newCart)
    //this.http.post('/api/cart/', newCart).subscribe();
  }
}
