import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { api } from '../../app.config';
import { Category } from '../../app.models';

@Injectable()
export class CategoryService {

    private categories: Category[] = [];

    constructor(private http: HttpClient) { }

    public getCategories(): Promise<Category[]> {
        if (this.categories.length) return Promise.resolve(this.categories);
        else {
            let promise = this.http.get<Category[]>(api.categories).toPromise();
            promise.then(cs => this.categories = cs);
            return promise;
        }
    }

    public getCategory(id: string): Promise<Category> {
        if (this.categories.length) return Promise.resolve(this.categories.filter(c => c.id === id)[0]);
        else return this.getCategories().then(cs => cs.filter(c => c.id === id)[0]);
    }

}
