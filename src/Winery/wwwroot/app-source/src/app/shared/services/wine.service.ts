import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { api } from '../../app.config';
import { Wine, Command } from '../../app.models';
import { Observable } from 'rxjs/Observable';
import { CommandKeys, WinehubService } from './winehub.service';
import { ProcessorService } from './processor.service';

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

    constructor(private http: HttpClient, private processor: ProcessorService, private winehub: WinehubService) {
        this.winehub.on('AddWine').subscribe(result => this.add(result.id));
        this.winehub.on('UpdateWine').subscribe(result => this.update(result.id));
        this.winehub.on('DeleteWine').subscribe(result => this.delete(result.id));
    }

    public getAllWines(): Promise<Wine[]> {
        return this.http.get<Wine[]>(api.allwines).toPromise();
    }

    public getWines(categoryId: string): Promise<Wine[]> {
        if (this.categoryId === categoryId && this.wines) return Promise.resolve(this.wines);
        else  return this.http.get<Wine[]>(api.wines(this.categoryId = categoryId)).toPromise().then(wines => this.wines = wines);
    }

    private getWineById(wineId: string): Observable<Wine> { 
        return this.http.get<Wine>(api.allwines + '/' + wineId);
    }

    public getWine(categoryId:string, wineId: string): Promise<Wine> {
        let filter = (w: Wine) => w.id === wineId;
        if (this.categoryId === categoryId && this.wines) return Promise.resolve(this.wines.filter(filter)[0]);
        else return this.getWines(categoryId).then(wines => wines.filter(filter)[0])
    }

    private completeProcess(process: CommandKeys) {
        let sub = this.winehub.on(process).subscribe(result => {
            this.processor.complete(result);
            sub.unsubscribe(); 
        });
    }

    public addWine(wine: NewWine): Promise<Command> {
        let promise = this.http.post<Command>(`${api.wines(this.categoryId)}`, wine).toPromise();
        promise.then(() => this.completeProcess('AddWine'), err => this.processor.complete(this.error(err)));
        return promise;
    }

    public editWine(wineId: string, wine: NewWine): Promise<Command> {
        let promise = this.http.put<Command>(`${api.wines(this.categoryId)}/${wineId}`, wine).toPromise();
        promise.then(() => this.completeProcess('AddWine'), err => this.processor.complete(this.error(err)));
        return promise;
    }

    public deleteWine(wineId: string): Promise<Command> {
        let promise = this.http.delete<Command>(api.wines(this.categoryId) + '/' + wineId).toPromise();
        promise.then(() => this.completeProcess('AddWine'), err => this.processor.complete(this.error(err)));
        return promise;
    }

    public error = (err: HttpErrorResponse) => err.error || err.message;
    
    private add(id: string): void {
        this.getWineById(id).toPromise()
            .then(wine => this.wines.push(wine));
    }

    private update(id: string): void {
        Promise
            .all([this.getWineById(id).toPromise(), this.getWine(this.categoryId, id)])
            .then(values => {
                let wineToUpdate  = values[1];
                if (wineToUpdate) {
                    let updateInfo = values[0];
                    console.log(updateInfo)
                    wineToUpdate.name = updateInfo.name;
                    wineToUpdate.description = updateInfo.description;
                    wineToUpdate.categoryID = updateInfo.categoryID;
                    wineToUpdate.imagePath = updateInfo.imagePath;
                    wineToUpdate.year = updateInfo.year;
                    wineToUpdate.price = updateInfo.price;
                }
            });            
    }

    private delete = (id: string) => this.getWine(this.categoryId, id).then(wine => {
        if (wine) this.wines.splice(this.wines.indexOf(wine), 1)
    });
    
}
