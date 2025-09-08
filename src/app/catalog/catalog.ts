import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { ProductDetails } from '../product-details/product-details';
import { CartService } from '../cart/cart-service';
import { ProductService } from './product-service';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Observable, OperatorFunction, filter, map, of } from 'rxjs';

@Component({
  selector: 'app-catalog',
  imports: [CommonModule, ProductDetails, RouterLink],
  templateUrl: './catalog.html',
  providers: [CartService],
  styleUrl: './catalog.css',
})
export class Catalog {
  products$!: Observable<Product[]>;
  filterValue: string = '';
  filteredProducts$!: Observable<Product[]>;

  ngOnInit() {
    this.products$ = this.productService.getProducts();

    this.route.params.subscribe((params) => {
      this.filterValue = params['filter']?.toLowerCase() ?? '';
 
      //working
      // this.filteredProducts$ = this.products$.pipe(value=> {
      //   return this.filterProducts(value);
      // });

      
      this.filteredProducts$ = this.products$.pipe(this.filterProductsOperator);
    });
  }

  constructor(
    private cartService: CartService,
    private productService: ProductService,
    private router: Router,
    private route: ActivatedRoute
  ) {}

  getImageUrl(product: Product) {
    return '/assets/images/' + product.imageName;
  }

  add(product: Product) {
    this.cartService.add(product);
    this.router.navigate(['/cart']);
  }

  private filterProducts (source:Observable<Product[]>) : Observable<Product[]> {
    return source.pipe(map((products: Product[]) =>
      this.filterValue
        ? products.filter((p) =>
            p.category.toLowerCase().includes(this.filterValue.toLowerCase())
          )
        : products
    ));
  }

  private filterProductsOperator: OperatorFunction<Product[], Product[]> = (source) =>
  source.pipe(
    map((products) =>
      this.filterValue
        ? products.filter((p) =>
            p.category.toLowerCase().includes(this.filterValue.toLowerCase())
          )
        : products
    )
  );
}
