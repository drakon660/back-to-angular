import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output, output } from '@angular/core';

@Component({
  selector: 'app-product-details',
  imports: [CommonModule],
  templateUrl: './product-details.html',
  styleUrl: './product-details.css',
})
export class ProductDetails {
  @Input() product!: Product;
  @Input() productType: string = 'robot-parts';
  @Output() buyEvent = new EventEmitter();

  getImageUrl(product: Product) {
    return `/assets/images/` + product.imageName;
  }

  buyButtonClicked(product: Product) {
    console.log('Adding to cart: ' + product.name);
    this.buyEvent.emit();
  }
}
