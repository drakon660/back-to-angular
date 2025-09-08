interface Product {
    id: number;
    name: string;
    description: string;
    imageName:string;
    category: string;
    price: number;
    discount: number;
}

interface CartItem {
    product: Product;
    quantity: number;
    total: number;
}