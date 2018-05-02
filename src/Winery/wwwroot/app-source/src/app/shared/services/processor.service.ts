import { Injectable } from '@angular/core';
import { CommandResult } from '../../app.models';


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
        // remove all command-complete classes
        this.child.classList.remove('bg-success');
        this.child.classList.remove('bg-danger');
        this.child.classList.remove('text-white');
    }

    public complete(commandResult: CommandResult): void {
        let newclass = commandResult.result.case === "Failure" ? 'bg-danger' : 'bg-success';
        this.child.classList.add(newclass);
        this.child.classList.add('text-white');
        this.child.classList.remove('bg-warning');
        this.child.innerText = commandResult.message;
        setTimeout(() => this.container.style.display = 'none', 5000);
    }

}
