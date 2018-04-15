import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { api } from '../../app.config';
import { Wine, Command } from '../../app.models';

type NewWine = {
    name: string
    description: string
    year: number
    price: number
    imagePath: string
}

@Injectable()
export class WineService {
    private wines: Wine[];
    private categoryId: string;

    constructor(private http: HttpClient) { }

    public getWines(categoryId: string): Promise<Wine[]> {
        if (this.categoryId === categoryId && this.wines) return Promise.resolve(this.wines);
        else  {
            this.categoryId = categoryId;
            return this.http.get<Wine[]>(api.wines(categoryId))
                       .toPromise().then(wines => this.wines = wines);
        }
    }

    public getWine(categoryId:string, wineId: string): Promise<Wine> {
        let filter = (w: Wine) => w.id === wineId;
        if (this.categoryId === categoryId && this.wines) return Promise.resolve(this.wines.filter(filter)[0]);
        else return this.getWines(categoryId).then(wines => wines.filter(filter)[0])
    }

    public addWine(categoryId: string, wine: NewWine): Promise<Command> {
        let promise = this.http.post<Command>(`${api.wines(categoryId)}`, wine).toPromise();
        promise.then(console.log);
        return promise;
    }

    public editWine(categoryId: string, wineId: string, wine: NewWine): Promise<Command> {
        let promise = this.http.post<Command>(`${api.wines(categoryId)}/${wineId}`, wine).toPromise();
        promise.then(console.log);
        return promise;
    }
}
