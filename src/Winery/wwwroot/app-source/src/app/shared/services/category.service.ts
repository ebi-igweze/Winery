import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { api } from '../../app.config';
import { Category, Command } from '../../app.models';

type CategoryInfo = { name: string, description: string }

@Injectable()
export class CategoryService {

    private categories: Category[];

    constructor(private http: HttpClient) { }

    public getCategories(): Promise<Category[]> {
        if (this.categories) return Promise.resolve(this.categories);
        else {
            let promise = this.http.get<Category[]>(api.categories).toPromise();
            promise.then(cs => this.categories = cs);
            return promise;
        }
    }

    public getCategory(id: string): Promise<Category> {
        if (this.categories) return Promise.resolve(this.categories.filter(c => c.id === id)[0]);
        else return this.getCategories().then(cs => cs.filter(c => c.id === id)[0]);
    }

    public addCategory(info: CategoryInfo): Promise<Command> {
        let promise = this.http.post<Command>(api.categories, info).toPromise();
        
        return promise;
    }

    public editCategory(id: string, info: CategoryInfo): Promise<Command> {
        return this.http.put<Command>(api.categories+'/'+id, info).toPromise();
    }

    public deleteCategory(id: string): Promise<Command> {
        return this.http.delete<Command>(api.categories+'/'+id).toPromise();
    }
}
