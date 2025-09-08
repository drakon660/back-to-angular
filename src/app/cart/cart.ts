import { Component } from '@angular/core';
import { CartService } from './cart-service';
import { CurrencyPipe, NgClass } from '@angular/common';

@Component({
  selector: 'app-cart',
  imports: [CurrencyPipe, NgClass],
  templateUrl: './cart.html',
  styleUrl: './cart.css',
})
export class Cart {
  cartItems: CartItem[] = [];

  get CarItem() {
    return this.cartItems;
  }

  constructor(private cartService: CartService) {
    this.cartService.getCart().subscribe((items) => {
      this.cartItems = items;
    });
  }

  getImageUrl(product: Product) {
    if (!product) return '';
    return '/assets/images/robot-parts/' + product.imageName;
  }

  get cartTotal() {
    return this.cartItems.reduce((prev, next) => {
      let discount =
        next.product.discount && next.product.discount > 0
          ? 1 - next.product.discount
          : 1;
      return prev + next.product.price * discount;
    }, 0);
  }

  removeFromCart(product: Product) {
    this.cartService.remove(product);
  }
}
