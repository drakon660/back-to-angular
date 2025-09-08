import { Component, Input, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { User } from './user';

@Component({
    imports: [FormsModule],
    selector: 'selector-name',
    templateUrl: './user-add.html'
})

export class UserComponent implements OnInit {
    @Input() user: User = {
        firstName:'',
        lastName:'',
        email:''       
    };

    constructor() { }

    ngOnInit() { }

    updateName() {
        this.user.firstName = 'Nancy';
    }
}