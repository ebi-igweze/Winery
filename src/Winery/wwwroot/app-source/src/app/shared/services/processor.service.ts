import { Injectable } from '@angular/core';


export type ProcessStatus = 'InProgress' | 'Failure' | 'Success';

export class Processor {
    private processing = false;
    private completed = true;
    private status: ProcessStatus = 'Success';

    public start(): void {
        this.processing = true;
        this.completed = false;
        this.status = 'InProgress';
       
    }

    public stop(status: ProcessStatus): void {
        this.processing = false;
        this.completed = false;
        this.status = status;
    }
}

@Injectable()
export class ProcessorService {
    private container: HTMLDivElement;
    private child: HTMLDivElement;

    constructor() {
        this.container = document.createElement('div');
        this.container.className = 'fixed-top w-100 text-center';
        this.child = document.createElement('div');
        this.child.className = 'd-inline-block bg-warning small px-3 py-1';
        this.container.appendChild(this.child);
        // hide container
        this.container.style.display = 'none';
        // append container to body
        document.body.appendChild(this.container);
    }

    public start(process: string): void {
        this.container.style.display = 'block';
        this.child.innerText = process+'...';
        this.child.classList.add('bg-warning');
        this.child.classList.remove('bg-success');
        this.child.classList.remove('text-white');
    }

    public stop(message: string): void {
        this.child.classList.add('bg-success');
        this.child.classList.add('text-white');
        this.child.classList.remove('bg-warning');
        this.child.innerText = message;
        setTimeout(() => this.container.style.display = 'none', 3000);
    }

}
