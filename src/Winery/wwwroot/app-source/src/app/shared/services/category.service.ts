import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { api } from '../../app.config';
import { Category, Command } from '../../app.models';
import { ProcessorService } from './processor.service';
import { WinehubService, CommandKeys } from './winehub.service';

type CategoryInfo = { name: string, description: string }

@Injectable()
export class CategoryService {

    private categories: Category[];
    private categoriesPromise: Promise<Category[]>

    constructor(private http: HttpClient, private winehub: WinehubService, private processor: ProcessorService) { 
        this.winehub.on('AddCategory').subscribe(result => this.add(result.id));
        this.winehub.on('DeleteCategory').subscribe(result => this.delete(result.id));
        this.winehub.on('UpdateCategory').subscribe(result => this.update(result.id));
        this.getCategories(); 
    }

    public getCategories(): Promise<Category[]> {
        if (this.categories) return Promise.resolve(this.categories);
        else if (this.categoriesPromise) return this.categoriesPromise
        else {
            this.categoriesPromise = this.http.get<Category[]>(api.categories).toPromise();
            this.categoriesPromise.then(cs => this.categories = cs);
            return this.categoriesPromise;
        }
    }

    public getCategory(id: string): Promise<Category> {
        return this.getCategories().then(cs => cs.filter(c => c.id === id)[0]);
    }

    private completeProcess(process: CommandKeys) {
        console.log('register complete process')
        let sub = this.winehub.on(process).subscribe(result => {
            this.processor.complete(result);
            console.log('complete process done.')
            sub.unsubscribe(); 
        });
    }

    public addCategory(info: CategoryInfo): Promise<Command> {
        this.processor.start('Adding new wine category');
        let promise = this.http.post<Command>(api.categories, info).toPromise();
        promise.then(() => this.completeProcess('AddCategory'));
        return promise;
    }

    public editCategory(id: string, info: CategoryInfo): Promise<Command> {
        this.processor.start('Editing wine category');
        let promise = this.http.put<Command>(api.categories+'/'+id, info).toPromise();
        promise.then(() => this.completeProcess('UpdateCategory'));
        return promise;
    }

    public deleteCategory(id: string): Promise<Command> {
        this.processor.start('Deleting wine category');
        let promise = this.http.delete<Command>(api.categories+'/'+id).toPromise();
        promise.then(() => this.completeProcess('DeleteCategory'));
        return promise;
    }

    private getCategoryById = (id: string) => this.http.get<Category>(api.categories+'/'+id);
    
    private add(id: string): void {
        this.getCategoryById(id).toPromise()
            .then(category => { this.categories.push(category)});
    }

    private update(id: string): void {
        Promise
            .all([this.getCategoryById(id).toPromise(), this.getCategory(id)])
            .then(values => {
                let updateInfo = values[0];
                let categoryToUpdate  = values[1];
                categoryToUpdate.name = updateInfo.name;
                categoryToUpdate.description = updateInfo.description;
            });            
    }

    private delete = (id: string) => this.getCategory(id).then(c => this.categories.splice(this.categories.indexOf(c), 1));
    

}
